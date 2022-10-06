﻿using System;
using DocumentFormat.OpenXml;
using ShapeCrawler.Shapes;
using ShapeCrawler.Tables;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler.Factories
{
    internal class TableGraphicFrameHandler : OpenXmlElementHandler
    {
        private const string Uri = "http://schemas.openxmlformats.org/drawingml/2006/table";

        internal override Shape Create(OpenXmlCompositeElement compositeElementOfPShapeTree, SCSlide slide, SlideGroupShape groupShape)
        {
            if (compositeElementOfPShapeTree is P.GraphicFrame pGraphicFrame)
            {
                var graphicData = pGraphicFrame.Graphic!.GraphicData!;
                if (!graphicData.Uri!.Value!.Equals(Uri, StringComparison.Ordinal))
                {
                    return this.Successor?.Create(compositeElementOfPShapeTree, slide, groupShape);
                }

                var table = new SlideTable(pGraphicFrame, slide, groupShape);

                return table;
            }

            return this.Successor?.Create(compositeElementOfPShapeTree, slide, groupShape);
        }
    }
}