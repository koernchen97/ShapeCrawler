﻿using ShapeCrawler.Models;
using Xunit;

// ReSharper disable TooManyChainedReferences

namespace ShapeCrawler.Tests.Unit
{
    public class TestFile_016
    {
        [Fact]
        public void SlidesCollection_Test()
        {
            // Arrange
            var pre = new Presentation(Properties.Resources._016);

            // Act-Assert
            var slides = pre.Slides; // should not throws exception
        }
    }
}
