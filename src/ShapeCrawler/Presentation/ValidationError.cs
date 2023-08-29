﻿namespace ShapeCrawler;

internal sealed class ValidationError
{
    internal ValidationError(string description, string path)
    {
        this.Description = description;
        this.Path = path;
    }

    internal string Path { get; }

    internal string Description { get; }
}