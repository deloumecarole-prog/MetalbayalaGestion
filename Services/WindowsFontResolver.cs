using System;
using System.IO;
using PdfSharp.Fonts;

namespace MetalBayalaGestion.Services;

/// <summary>
/// PDFsharp 6.x ne detecte plus automatiquement les polices Windows installees :
/// il faut fournir un IFontResolver explicite. Celui-ci lit les fichiers .ttf
/// directement dans le dossier Fonts de Windows (C:\Windows\Fonts).
/// </summary>
public class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    public string DefaultFontName => "Arial";

    public byte[]? GetFont(string faceName)
    {
        var path = faceName switch
        {
            "Arial#Regular" => Path.Combine(FontsFolder, "arial.ttf"),
            "Arial#Bold" => Path.Combine(FontsFolder, "arialbd.ttf"),
            "Arial#Italic" => Path.Combine(FontsFolder, "ariali.ttf"),
            "Arial#BoldItalic" => Path.Combine(FontsFolder, "arialbi.ttf"),
            _ => Path.Combine(FontsFolder, "arial.ttf")
        };

        if (File.Exists(path))
            return File.ReadAllBytes(path);

        // Repli sur Segoe UI si Arial est introuvable (present sur tout Windows 10/11 recent)
        var fallback = faceName switch
        {
            "Arial#Bold" => "segoeuib.ttf",
            "Arial#Italic" => "segoeuii.ttf",
            "Arial#BoldItalic" => "segoeuiz.ttf",
            _ => "segoeui.ttf"
        };
        var fallbackPath = Path.Combine(FontsFolder, fallback);
        return File.Exists(fallbackPath) ? File.ReadAllBytes(fallbackPath) : null;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var suffix = (isBold, isItalic) switch
        {
            (true, true) => "BoldItalic",
            (true, false) => "Bold",
            (false, true) => "Italic",
            _ => "Regular"
        };
        return new FontResolverInfo($"Arial#{suffix}");
    }
}
