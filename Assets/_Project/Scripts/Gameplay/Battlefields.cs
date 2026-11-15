using System.Collections.Generic;

namespace Eldoria.Gameplay
{
    /// <summary>A map the player can fight on: its name, the scene it's built in, and a short description.</summary>
    public class Battlefield
    {
        public string Name;
        public string Scene;
        public string Description;
    }

    /// <summary>The maps the player can choose from after pressing Play (each scene must be in the build settings).</summary>
    public static class Battlefields
    {
        public static readonly Battlefield UrbanDemo = new Battlefield
        {
            Name = "Boxed in",
            Scene = "UrbanDemo",
            Description = "80 × 60 m city block around a central plaza, laid out with 180° symmetry. Roofless buildings with " +
                          "doorways and climbable windows, streets full of cars, sandbags and crates.",
        };

        public static readonly Battlefield EnglishCountryside = new Battlefield
        {
            Name = "Open Season",
            Scene = "Countryside",
            Description = "100 × 100 m of open, rolling fields. A knoll, a long ridge and a low rise block line of sight; a river " +
                          "crosses the map (wading costs double, or use the stone bridge or the ford); a farmhouse, a barn, a " +
                          "stone shed, and hay bales, carts and fences for cover.",
        };

        public static readonly Battlefield FortSolstice = new Battlefield
        {
            Name = "Siege of Ft Solstice",
            Scene = "FortSolstice",
            Description = "100 \u00d7 130 m of desert, asymmetrical: the south side attacks across 42 m of open sand, the north defends a " +
                          "massive fortress. Three lanes lead through the outer wall and the middle wall to the keep. The " +
                          "attackers win by getting a unit leader onto the objective in the centre of the keep.",
        };

        public static readonly Battlefield PolarNonsense = new Battlefield
        {
            Name = "Polar Nonsense",
            Scene = "Polar",
            Description = "100 × 100 m of snow, laid out with 180° symmetry. Snowy knolls, ridges and rises block line of " +
                          "sight; a small polar station in the centre and on each side; snowbanks, fuel drums and crates for " +
                          "half-cover, ice boulders and fuel tanks for full cover.",
        };

        public static readonly Battlefield LeisureCruise = new Battlefield
        {
            Name = "Leisure Cruise",
            Scene = "Ship",
            Description = "The cluttered inside of a ship, 50 \u00d7 150 m: deploy in the hangar bays at either end and fight " +
                          "down a wide spine corridor, narrow side corridors and the compartments between, through a cargo " +
                          "hold in the middle. Crates, drums, debris and containers everywhere.",
        };

        public static readonly IReadOnlyList<Battlefield> All = new[] { UrbanDemo, EnglishCountryside, FortSolstice, PolarNonsense, LeisureCruise };
    }
}
