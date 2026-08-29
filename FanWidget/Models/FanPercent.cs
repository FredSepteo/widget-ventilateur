namespace FanWidget.Models;

public static class FanPercent
{
    /// <summary>En dessous de ce seuil (hors 0 %), les hélices ne démarrent généralement pas.</summary>
    public const int MinSpinPercent = 30;

    public static int Snap(double value, int minSliderPercent = 0, bool enforceMinSpin = true)
    {
        if (value <= 0 && minSliderPercent <= 0)
            return 0;

        var floor = minSliderPercent;
        if (enforceMinSpin)
            floor = Math.Max(floor, MinSpinPercent);

        var snapped = (int)(Math.Round(Math.Clamp(value, floor, 100) / 10.0) * 10);
        return Math.Max(snapped, floor);
    }

    /// <summary>Arrondi affichage seulement (lecture matérielle, mode auto).</summary>
    public static int SnapDisplay(double value) =>
        (int)(Math.Round(Math.Clamp(value, 0, 100) / 10.0) * 10);
}
