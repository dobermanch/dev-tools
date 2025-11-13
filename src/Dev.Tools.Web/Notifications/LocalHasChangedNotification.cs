using System.Globalization;

namespace Dev.Tools.Web.Notifications;

internal record LocalHasChangedNotification(CultureInfo Local) : IMessage;