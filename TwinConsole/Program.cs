using System;
using TwinYields;
using System.Linq;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using System.Collections.Generic;
using TwinYields.DataBase;
using System.IO;
using Models.PMF;

namespace TwinConsole;


class Program
{
    static void Main(string[] args)
    {
        //TODO get files from DB
        string simFile = "simulations/wheat_zones_RVIII.apsimx";
        var cmd = "";

        if (args.Length > 0)
            cmd = args[0];

        //cmd = "init";
        switch (cmd)
        {
            case "init":
                var status = InitializeTwin();
                break;
            case "run":
                Run(simFile);
                break;
            case "optim":
                OptimizeParams(simFile);
                break;
            default:
                Console.WriteLine("No command provided, defaulting to run");
                Run(simFile);
                break;
        }
    }

    static bool InitializeTwin()
    {
        Console.WriteLine("Initializing DigitalTwin from database");
        var db = new TwinDataBase();
        db.DropSimulationFiles();
        var Fields = db.FindFields();
        
        foreach (var field in Fields)
        {
            var zones = field.Zones;
            Directory.CreateDirectory("simulations");
            Console.WriteLine($"\t Processing field: {field.Name}");

            string protopath = @"prototypes/WheatProto.apsimx";
            string outName = $"simulations/wheat_zones_{field.Name}.apsimx";

            var sb = new APSIMBuilder();
            var simulations = sb.BuildSimulations(zones, protopath, outName);
            db.Insert(new SimulationFile(field.Name, outName));
        }

        Console.WriteLine("Initializion complete");
        return true;
    }

    static void OptimizeParams(string simFile)
    {
        var cmds = File.ReadAllLines("optim/cultivar.txt");
        Console.WriteLine("Optimizing parameters!");
        Simulations sims = FileFormat.ReadFromFile<Simulations>(simFile, e => throw e, false);
        foreach (var sim in sims.FindAllChildren<Simulation>())
        {
            var zone = sim.FindChild<Models.Core.Zone>();
            var cultivar = zone.Plants.First().FindChild<Cultivar>();
            cultivar.Command = cmds;
        }

        var json = FileFormat.WriteToString(sims);
        File.WriteAllText("optim/OptSim.apsimx", json);

        var srunner = new Runner("optim/OptSim.apsimx");
        srunner.Run();
        srunner.DisposeStorage();
        Console.WriteLine("Done running!");
    }

    static void Run(string simFile)
    {
        Console.WriteLine("Running simulations!");
        Simulations sims = FileFormat.ReadFromFile<Simulations>(simFile, e => throw e, false);
        //var sim1 = sims.FindChild<Simulation>();
        //sim1.Run();
        //var d = data.Reader.GetData("Simulation");

        var srunner = new Runner(sims);
        srunner.Run();
        srunner.DisposeStorage();

        //Use this to read the simulated data back
        //var data = sims.FindChild<DataStore>();
        //data.Open();
        //var tables = data.Reader.TableNames;
        //var dt = data.Reader.GetData(tables.First());
        //Console.WriteLine(dt.Rows.Count);

        //var dt2 = data.Reader.GetData("*", "Current", data.Reader.SimulationNames);
       Console.WriteLine("Done running!");

        //Run the simFiles
        //foreach (var simF in simFiles)
        //{
        //    Runner runner = new Runner(simF);
        //    runner.Run();
        //}



    }

}