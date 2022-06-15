﻿using System;
using TwinYields;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using System.Collections.Generic;
using TwinYields.DataBase;
using System.IO;

namespace TwinConsole;


class Program
{
    static void Main(string[] args)
    {
        string simFile = "simulations/wheat_zones.apsimx";
        //if (args.Length == 0)
        simFile = InitializeTwin();
        Run(simFile);
        Console.WriteLine("End");
    }

    static string InitializeTwin()
    {
        //var importPath = "TASKDATA_20210603_0159";
        var importPath = "TASKDATA_20220520_0906";
        var task = new AdaptConverter(importPath);
        
        var rates = task.VectorizePrescription();
        //string time = DateTime.Now.ToString("hh_mm_ss");
        //task.RasterizePrescription($"geoms/rasters/{task.FieldName}_{time}.tiff");
        var zones = task.PrescriptionZones();

        //Serialize the zones to GeoJSON
        //AdaptConverter.SaveJSON(zones, "geoms/zones.json");
        //AdaptConverter.SaveJSON(task.FieldBoundary, "geoms/field.json");
        //Can be rasterized using gdal: gdal_rasterize -a rate -ot Int16 -ts 1000 1000 rates.json zones.tif
        //AdaptConverter.SaveJSON(rates, "geoms/rates.json");

        //Save field information to database

        var db = new TwinDataBase();
        db.DropAll();
        var farm = new Farm("Jokioinen SmartFarm");
        db.Insert(farm);
        var field = new Field(task.FieldName, task.FieldBoundary);
        field.Zones = zones.Select((z, i) => 
                            new TwinYields.DataBase.Zone($"zone{i}", 
                            new double[] { (double)z.Attributes["rate"] }, //TODO get all rates and product types
                            task.Products,
                            (Polygon)z.Geometry)).ToList();
        field.FarmId = farm.Id;
        db.Insert(field);

        Console.WriteLine("Done with DB");
       

        //Test reading
        //var ofield = db.FindField(task.FieldName);

        //TODO extract variable rates from operations
        //var operations = task.GroupOperations();

        Console.WriteLine("Done with taskfile");
        //Convert to APSIM simulations
        //Directory.Delete("simulations", true);
        Directory.CreateDirectory("simulations");

        string protopath = @"prototypes/WheatProto.apsimx";
        string outName = "simulations/wheat_zones.apsimx";
        var sb = new APSIMBuilder();
        var simulations = sb.BuildSimulations(zones, protopath, outName);
        //var simFiles = sb.BuildSimulationFiles(zones, protopath);
        return outName;

    }

    static void Run(string simFile)
    {
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