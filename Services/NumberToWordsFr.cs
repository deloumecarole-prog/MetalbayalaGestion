using System;
using System.Collections.Generic;

namespace MetalBayalaGestion.Services;

/// <summary>
/// Convertit un montant numerique en toutes lettres en francais, pour l'affichage
/// du type "Arrete la presente facture a la somme de (en FCFA): Trente mille"
/// sur les documents PDF (devis, factures, bons de livraison).
/// </summary>
public static class NumberToWordsFr
{
    private static readonly string[] Units =
    {
        "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
        "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
        "dix-sept", "dix-huit", "dix-neuf"
    };

    private static readonly string[] Tens =
    {
        "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "", "quatre-vingt", ""
    };

    /// <summary>
    /// Convertit un nombre entier de 0 a 999 en lettres.
    /// isMultiplier = true quand ce nombre precede "mille"/"million"/"milliard" :
    /// dans ce cas "vingt" et "cent" ne prennent jamais de s final (regle francaise).
    /// </summary>
    private static string ConvertBelow1000(int n, bool isMultiplier = false)
    {
        if (n == 0) return "";
        if (n < 20) return Units[n];

        if (n < 100)
        {
            int tensDigit = n / 10;
            int unit = n % 10;

            // 70-79 : soixante-dix a soixante-dix-neuf (avec "et" pour 71)
            // 90-99 : quatre-vingt-dix a quatre-vingt-dix-neuf (jamais de "et")
            if (tensDigit == 7 || tensDigit == 9)
            {
                string basePart = tensDigit == 7 ? "soixante" : "quatre-vingt";
                if (tensDigit == 7 && unit == 1)
                    return basePart + "-et-onze";
                int remainder = 10 + unit;
                return basePart + "-" + Units[remainder];
            }

            string tensWord = Tens[tensDigit];
            if (unit == 0)
            {
                // "quatre-vingts" prend un s seulement s'il n'est suivi de rien d'autre
                return (tensDigit == 8 && !isMultiplier) ? tensWord + "s" : tensWord;
            }
            if (unit == 1 && tensDigit != 8)
                return tensWord + "-et-un";

            return tensWord + "-" + Units[unit];
        }

        // 100-999
        int hundreds = n / 100;
        int rest = n % 100;
        string result = hundreds == 1 ? "cent" : Units[hundreds] + "-cent";
        if (rest == 0)
        {
            // "deux-cents" mais "deux-cent-un" (sans s) et jamais de s si suivi de mille/million
            if (hundreds > 1 && !isMultiplier) result += "s";
        }
        else
        {
            result += "-" + ConvertBelow1000(rest, isMultiplier);
        }
        return result;
    }

    /// <summary>Convertit un entier positif (jusqu'aux milliards) en lettres completes.</summary>
    private static string ConvertInteger(long n)
    {
        if (n == 0) return "zéro";

        var parts = new List<string>();

        long milliards = n / 1_000_000_000; n %= 1_000_000_000;
        long millions = n / 1_000_000; n %= 1_000_000;
        long milliers = n / 1000; n %= 1000;
        long reste = n;

        if (milliards > 0)
            parts.Add(milliards == 1 ? "un-milliard" : ConvertBelow1000((int)milliards, true) + "-milliards");

        if (millions > 0)
            parts.Add(millions == 1 ? "un-million" : ConvertBelow1000((int)millions, true) + "-millions");

        if (milliers > 0)
            parts.Add(milliers == 1 ? "mille" : ConvertBelow1000((int)milliers, true) + "-mille");

        if (reste > 0)
            parts.Add(ConvertBelow1000((int)reste, false));

        return string.Join("-", parts);
    }

    /// <summary>
    /// Convertit un montant en toutes lettres, premiere lettre en majuscule,
    /// suffixe "Francs CFA". Exemple : 30000 -> "Trente mille Francs CFA"
    /// </summary>
    public static string ToWords(decimal amount)
    {
        var rounded = (long)Math.Round(Math.Abs(amount), MidpointRounding.AwayFromZero);
        var words = ConvertInteger(rounded).Replace("-", " ");
        words = char.ToUpper(words[0]) + words.Substring(1);
        return $"{words} Francs CFA";
    }
}
