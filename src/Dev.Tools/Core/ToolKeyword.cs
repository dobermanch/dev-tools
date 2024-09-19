﻿namespace Dev.Tools.Core;

public readonly record struct ToolKeyword
{
    public const string Convert = nameof(Convert);
    public const string String = nameof(String);
    public const string Text = nameof(Text);
    public const string Decode = nameof(Decode);
    public const string Encode = nameof(Encode);
    public const string Base64 = nameof(Base64);
    public const string Misc = nameof(Misc);
    public const string Url = nameof(Url);
    public const string Uuid = nameof(Uuid);
    public const string Generate = nameof(Generate);
    public const string Guid = nameof(Guid);
}


public readonly record struct ToolConstants
{
    public readonly record struct Category
    {
        public const string Converter = nameof(Converter);
        public const string Text = nameof(Text);
        public const string Misc = nameof(Misc);
    }

    public readonly record struct Keyword
    {
        public const string Convert = nameof(Convert);
        public const string String = nameof(String);
        public const string Text = nameof(Text);
        public const string Decode = nameof(Decode);
        public const string Encode = nameof(Encode);
        public const string Base64 = nameof(Base64);
        public const string Misc = nameof(Misc);
        public const string Url = nameof(Url);
        public const string Uuid = nameof(Uuid);
        public const string Generate = nameof(Generate);
        public const string Guid = nameof(Guid);
    }

    public readonly record struct Error
    {
        public const string Unknown = nameof(Unknown);
    }
}