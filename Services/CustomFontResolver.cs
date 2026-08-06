using System;
using System.IO;
using PdfSharp.Fonts;

namespace MetalBayalaGestion.Services;

public class CustomFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        var fileName = faceName switch
        {
            "Arial#Bold" => "arialbd.ttf",
            "Arial#Italic" => "ariali.ttf",
            "Arial#BoldItalic" => "arialbi.ttf",
            _ => "arial.ttf"
        };

        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Fichier de police introuvable : {path}. Vérifie que les .ttf sont bien copiés dans Resources/Fonts et marqués 'Copy if newer'.");

        return File.ReadAllBytes(path);
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (isBold && isItalic)
            return new FontResolverInfo("Arial#BoldItalic");
        if (isBold)
            return new FontResolverInfo("Arial#Bold");
        if (isItalic)
            return new FontResolverInfo("Arial#Italic");

        return new FontResolverInfo("Arial");
    }
}
