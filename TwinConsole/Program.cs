using System;
using TwinYields;
using AgGateway.ADAPT.ApplicationDataModel.ADM;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.IO;
using Newtonsoft.Json;
using NetTopologySuite.Features;
//using Deedle;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using System.Collections.Generic;

namespace TwinConsole;

class Program
{
    static void Main(string[] args)
    {
        var importPath = "TASKDATA_20210603_0159";
        var task = new AdaptConverter(importPath);
        
        //Get task properties using ADAPT
        var field = task.FieldBoundaries();
        var zones = task.PrescriptionZones();
        //var frame = task.PrescriptionFrame();
        //var idx = frame.IndexRowsUsing(r => (r.Get("rate0"), r.Get("rate1")));
        var operations = task.GroupOperations();

        //Convert to APSIM simulations
        Directory.Delete("simulations", true);
        Directory.CreateDirectory("simulations");
        
        string protopath = @"prototypes/WheatProto.apsimx";
        string outName = "simulations/wheat_zones.apsimx";
        var sb = new APSIMBuilder();
        var simulations = sb.BuildSimulations(zones, protopath, outName);
        var simFiles = sb.BuildSimulationFiles(zones, protopath);
      
        var srunner = new Runner(outName);
        srunner.Run();
        srunner.DisposeStorage();
            
        //Run the simFiles
        foreach (var simF in simFiles)
        {
            Runner runner = new Runner(simF);
            runner.Run();
        }

        //Serialize the zones to GeoJSON
        var serializer = GeoJsonSerializer.Create();
        string geoJson;
        using (var stringWriter = new StringWriter())
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            serializer.Serialize(jsonWriter, zones);
            geoJson = stringWriter.ToString();
        }
        File.WriteAllText("geoms/zones.json", geoJson);



        Console.WriteLine("End");

    }
}