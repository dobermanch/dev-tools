#!/usr/bin/env bash
# Stops this dev container once nobody has been connected to it for a while.
#
# "shutdownAction": "stopContainer" already stops the container when the last
# VS Code window closes cleanly, but a crash, a laptop going to sleep or a
# dropped Remote-SSH link to the Docker host skips that step and leaves the
# container running forever. This watchdog covers those cases.
#
# Started as root by this feature's entrypoint on every container start,
# whoever starts it (VS Code, Portainer, docker start). Not from
# postStartCommand: VS Code does not let a process started there outlive it.
#
# A session is a person connected right now:
#   - a VS Code window: an established TCP connection to the VS Code server's
#     listening port (the window's tunnel ends in one). Process lists are no
#     use for this: the server, its extension hosts and the extension's
#     `docker exec` shells all outlive the window, sometimes for hours, so a
#     container would look busy forever.
#   - an interactive `docker exec -it … bash`: a PPID 0 process (its parent is
#     outside the container's PID namespace) holding a terminal.
#   - an SSH login, for containers that run sshd.
#
# Settings (containerEnv; remoteEnv is not seen at container start):
#   IDLE_SHUTDOWN_MINUTES  idle minutes before stopping; 0 disables (default 30)
# Keep a container up after you disconnect (a long training run, say) with
#   touch /tmp/stay-awake        and   rm /tmp/stay-awake   once it is done.

set -u

IDLE_MINUTES="${IDLE_SHUTDOWN_MINUTES:-30}"
CHECK_SECONDS=60

[ "$IDLE_MINUTES" -gt 0 ] 2>/dev/null || { echo "idle-shutdown disabled"; exit 0; }

# One watchdog per container, even if the entrypoint runs more than once.
# Not in /tmp: docker-in-docker mounts a fresh tmpfs there after this starts.
exec 9>/var/log/idle-shutdown.lock
flock -n 9 || exit 0

# Established connections to any port the VS Code server listens on. Ports
# are compared as /proc/net/tcp prints them, in hex.
vscode_connections() {
  local inodes
  inodes=$(for p in $(pgrep -f 'vscode-server/.*server-main\.js'); do
    ls -l "/proc/$p/fd" 2>/dev/null
  done | sed -n 's/.*socket:\[\([0-9]*\)\].*/\1/p')
  [ -n "$inodes" ] || { echo 0; return; }
  awk -v inodes="$inodes" '
    BEGIN { n = split(inodes, a, "\n"); for (i = 1; i <= n; i++) mine[a[i]] = 1 }
    FNR == 1 { next }
    { split($2, addr, ":"); port = addr[2] }
    $4 == "0A" && ($10 in mine) { listening[port] = 1 }
    $4 == "01" { established[++e] = port }
    END { c = 0; for (i = 1; i <= e; i++) if (established[i] in listening) c++; print c }
  ' /proc/net/tcp /proc/net/tcp6
}

sessions() {
  local vscode terminals ssh
  vscode=$(vscode_connections)
  terminals=$(ps -eo pid=,ppid=,tty= | awk '$2 == 0 && $1 != 1 && $3 != "?"' | wc -l)
  ssh=$(pgrep -fc 'sshd: [^ ]+@' || true)
  echo "vscode=$vscode terminals=$terminals ssh=$ssh"
}

echo "$(date -Is) idle-shutdown: stopping after ${IDLE_MINUTES}m with no sessions"
# Measured from the last check that saw a session. The session may have ended
# any time up to one interval after that check, so one interval is added to be
# sure the container really sat idle for IDLE_MINUTES.
last_busy=$(date +%s)
state=
while sleep "$CHECK_SECONDS"; do
  now=$(sessions)
  if [ "$now" != "$state" ]; then
    echo "$(date -Is) idle-shutdown: $now"
    state=$now
  fi
  if [ -e /tmp/stay-awake ] || [ "$now" != "vscode=0 terminals=0 ssh=0" ]; then
    last_busy=$(date +%s)
    continue
  fi
  if [ $(($(date +%s) - last_busy)) -ge $((IDLE_MINUTES * 60 + CHECK_SECONDS)) ]; then
    echo "$(date -Is) idle-shutdown: no sessions for ${IDLE_MINUTES}m, stopping container"
    # Stopping PID 1 stops the container.
    kill -TERM 1
    exit 0
  fi
done
