using System;
using System.Linq;
using System.IO;
namespace TwinYields;
using Models.Core;
using Models.Core.Run;
using Models.Core.ApsimFile;
using Models.PMF;

public class APSIMOptimizer
{
    public static void OptimizeCultivar(String simFile, String phenology)
    {
        Console.WriteLine("Optimizing parameters!");
        Simulations sims = FileFormat.ReadFromFile<Simulations>(simFile, e => throw e, false);
        foreach (var sim in sims.FindAllChildren<Simulation>())
        {
            var zone = sim.FindChild<Models.Core.Zone>();
            var cultivar = zone.Plants.First().FindChild<Cultivar>();
            cultivar.Command = phenology.Split('\n');
        }

        var srunner = new Runner(sims);
        srunner.Run();
        srunner.DisposeStorage();
        Console.WriteLine("Done running!");
    }
}