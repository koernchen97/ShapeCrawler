using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using ShapeCrawler.Exceptions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler.ShapeCollection;

internal sealed class ShapeGeometry : IShapeGeometry
{
    private static readonly Dictionary<Geometry, ShapeTypeValues> GeometryToShapeTypeValuesMap = new()
    {
        { Geometry.RoundedRectangle, A.ShapeTypeValues.RoundRectangle },
        { Geometry.SingleCornerRoundedRectangle, A.ShapeTypeValues.Round1Rectangle },
        { Geometry.TopCornersRoundedRectangle, A.ShapeTypeValues.Round2SameRectangle },
        { Geometry.DiagonalCornersRoundedRectangle, A.ShapeTypeValues.Round2DiagonalRectangle },
        { Geometry.UTurnArrow, A.ShapeTypeValues.UTurnArrow },
        { Geometry.LineInverse, A.ShapeTypeValues.LineInverse },
        { Geometry.RightTriangle, A.ShapeTypeValues.RightTriangle },
    };
    
    private static readonly Dictionary<ShapeTypeValues, Geometry> ShapeTypeValuesToGeometryMap 
        = GeometryToShapeTypeValuesMap.ToDictionary(x => x.Value, x => x.Key);

    private readonly P.ShapeProperties pShapeProperties;

    internal ShapeGeometry(P.ShapeProperties pShapeProperties)
    {
        this.pShapeProperties = pShapeProperties;
    }

    public Geometry GeometryType 
    { 
        get
        {
            var preset = this.APresetGeometry?.Preset;
            if (preset is null)
            {
                if (this.pShapeProperties.OfType<A.CustomGeometry>().Any())
                {
                    return Geometry.Custom;
                }
                else
                {
                    return Geometry.Rectangle;
                }
            }
            else
            {
                if (!ShapeTypeValuesToGeometryMap.TryGetValue(preset, out Geometry geometryType))
                {
                    var presetString = preset!.ToString() !;
                    var name = presetString.ToLowerInvariant().Replace("rect", "rectangle").Replace("diag", "diagonal");
                    if (!Enum.TryParse(name, true, out geometryType))
                    {
                        throw new SCException($"Unable to parse {name}");
                    }
                }

                return geometryType;
            }            
        }
        
        set
        {
            if (value == Geometry.Custom)
            {
                throw new SCException("Can't set custom geometry");
            }

            var aPresetGeometry = this.APresetGeometry;
            if (aPresetGeometry?.Preset is null && this.pShapeProperties.OfType<A.CustomGeometry>().Any())
            {
                throw new SCException("Can't set new geometry on a shape with custom geometry");
            }

            aPresetGeometry ??= this.pShapeProperties.InsertAt<A.PresetGeometry>(new(), 0)
                ?? throw new SCException("Unable to add new preset geometry");

            if (!GeometryToShapeTypeValuesMap.TryGetValue(value, out var newPreset))
            {
                var name = value.ToString().Replace("Rectangle", "Rect").Replace("Diagonal", "Diag");
                var camelName = ToCamelCaseInvariant(name);        
                newPreset = new ShapeTypeValues(camelName);
            }

            if (!(newPreset as IEnumValue).IsValid)
            {
                throw new SCException($"Invalid preset value {newPreset}");
            }

            aPresetGeometry.Preset = newPreset;

            // Presets have different expectations of an adjusted value lists, so changing the
            // preset means we must remove any existing adjustments, and create a new empty one
            aPresetGeometry.RemoveAllChildren<A.AdjustValueList>();
            aPresetGeometry.AppendChild<A.AdjustValueList>(new());
        }
    }

    public decimal? CornerSize
    {
        get
        {
            var shapeType = this.APresetGeometry?.Preset?.Value;

            if (shapeType == A.ShapeTypeValues.RoundRectangle)
            {
                return this.ExtractCornerSizeFromRoundRectangle();
            }

            if (shapeType == A.ShapeTypeValues.Round2SameRectangle)
            {
                return this.ExtractCornerSizeFromRound2SameRectangle();
            }

            return null;
        }
        
        set
        {
            if (value is null)
            {
                throw new SCException("Not allowed to set null size. Try 0 to straighten the corner.");
            }

            var shapeType = this.APresetGeometry?.Preset?.Value;

            if (shapeType == A.ShapeTypeValues.RoundRectangle)
            {
                this.InjectCornerSizeIntoRoundRectangle(value.Value);
            }

            if (shapeType == A.ShapeTypeValues.Round2SameRectangle)
            {
                this.InjectCornerSizeIntoRound2SameRectangle(value.Value);
            }
        }
    }

    private A.PresetGeometry? APresetGeometry => this.pShapeProperties.GetFirstChild<A.PresetGeometry>();

    private static string ToCamelCaseInvariant(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

#if NETSTANDARD2_0
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
#else
        return char.ToLowerInvariant(value[0]) + value[1..];
#endif
    }

    private static decimal ExtractCornerSizeFromShapeGuide(A.ShapeGuide sg)
    {
        var formula = sg.Formula?.Value ?? throw new SCException("Malformed rounded rectangle. Shape guide has no formula. Please file a GitHub issue.");

        var pattern = "^val (?<value>[0-9]+)$";

#if NETSTANDARD2_0
        var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(100));
#else
        var regex = new Regex(pattern, RegexOptions.NonBacktracking);
#endif

        var match = regex.Match(formula);
        if (!match.Success)
        {
            throw new SCException("Malformed rounded rectangle. Formula has no value. Please file a GitHub issue.");
        }

        var value = int.Parse(match.Groups["value"].Value);

        return value / 50000m;
    }

    private decimal? ExtractCornerSizeFromRoundRectangle()
    {
        var aPresetGeometry = this.APresetGeometry;
        if (aPresetGeometry?.Preset?.Value != A.ShapeTypeValues.RoundRectangle)
        {
            return null;
        }

        var avList = aPresetGeometry.AdjustValueList ?? throw new SCException("Malformed rounded rectangle. Missing AdjustValueList. Please file a GitHub issue.");
        var sgs = avList.Descendants<A.ShapeGuide>().Where(x => x.Name == "adj");
        if (!sgs.Any())
        {
            // Has no shape guide. That means we're using the DEFAULT value, which is 0.35
            return 0.35m;
        }

        if (sgs.Count() > 1)
        {
            throw new SCException("Malformed rounded rectangle. Has multiple shape guides. Please file a GitHub issue.");
        }

        return ExtractCornerSizeFromShapeGuide(sgs.Single());
    }

    private void InjectCornerSizeIntoRoundRectangle(decimal value)
    {
        var aPresetGeometry = this.APresetGeometry;
        if (aPresetGeometry?.Preset?.Value != A.ShapeTypeValues.RoundRectangle)
        {
            return;
        }

        var avList = aPresetGeometry.AdjustValueList ?? throw new SCException("Malformed rounded rectangle. Missing AdjustValueList. Please file a GitHub issue.");
        var sgs = avList.Descendants<A.ShapeGuide>().Where(x => x.Name == "adj");
        if (sgs.Count() > 1)
        {
            throw new SCException("Malformed rounded rectangle. Has multiple shape guides. Please file a GitHub issue.");
        }

        // Will add a shape guide if there isn't already one
        var sg = sgs.SingleOrDefault()
            ?? avList.AppendChild(new A.ShapeGuide() { Name = "adj" }) 
            ?? throw new SCException("Failed attempting to add a shape guide to AdjustValueList");

        sg.Formula = new StringValue($"val {(int)(value * 50000m)}");        
    }

    private decimal? ExtractCornerSizeFromRound2SameRectangle()
    {
        var aPresetGeometry = this.APresetGeometry;
        if (aPresetGeometry?.Preset?.Value != A.ShapeTypeValues.Round2SameRectangle)
        {
            return null;
        }

        var avList = aPresetGeometry.AdjustValueList ?? throw new SCException("Malformed rounded rectangle. Missing AdjustValueList. Please file a GitHub issue.");
        var sgs = avList.Descendants<A.ShapeGuide>();
        var count = sgs.Count();
        if (count == 0)
        {
            // Has no shape guide. That means we're using the DEFAULT value, which is 0.35
            return 0.35m;
        }

        if (count != 2)
        {
            throw new SCException($"Malformed rounded rectangle. Expected 2 shape guides, found {count}. Please file a GitHub issue.");
        }

        var sg = sgs.SingleOrDefault(x => x.Name == "adj1") ?? throw new SCException($"Malformed rounded rectangle. No shape guide named `adj1`. Please file a GitHub issue.");

        return ExtractCornerSizeFromShapeGuide(sg);
    }

    private void InjectCornerSizeIntoRound2SameRectangle(decimal value)
    {
        var aPresetGeometry = this.APresetGeometry;
        if (aPresetGeometry?.Preset?.Value != A.ShapeTypeValues.Round2SameRectangle)
        {
            return;
        }

        var avList = aPresetGeometry.AdjustValueList ?? throw new SCException("Malformed rounded rectangle. Missing AdjustValueList. Please file a GitHub issue.");
        var sgs = avList.Descendants<A.ShapeGuide>().Where(x => x.Name == "adj1");
        if (sgs.Count() > 1)
        {
            throw new SCException("Malformed rounded rectangle. Has multiple shape guides. Please file a GitHub issue.");
        }

        var sg = sgs.SingleOrDefault();
        if (sg is null)
        {
            // Has no adj1 shape guide. We need to add an adj1/adj2 pair
            sg = avList.AppendChild(new A.ShapeGuide() { Name = "adj1" }) ?? throw new SCException("Failed attempting to add a shape guide to AdjustValueList");
            if (avList.AppendChild(new A.ShapeGuide() { Name = "adj2", Formula = "val 0" }) is null)
            {
                throw new SCException("Failed attempting to add a shape guide to AdjustValueList");
            }
        }
    
        sg.Formula = new StringValue($"val {(int)(value * 50000m)}");        
    }
}