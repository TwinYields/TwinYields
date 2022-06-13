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
//using TwinYields.DB.Models;
//using Zone = TwinYields.DB.Models.Zone;
//using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace TwinConsole;

public class MongoZone
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public GeoJsonPolygon<GeoJson2DCoordinates> Geometry { get; set; }

    public MongoZone(string Name, NetTopologySuite.Geometries.Geometry Geometry)
    {
        var gf = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
        int N = Geometry.Coordinates.Count();
        var mongocrds = new GeoJson2DCoordinates[N];
        for (int i = 0; i < N; i++)
        {
            mongocrds[i] = GeoJson.Position(Geometry.Coordinates[i].X, Geometry.Coordinates[i].Y);
        }
        var polygon = GeoJson.Polygon(mongocrds);
        this.Name = Name;
        this.Geometry = polygon;
    }
}


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
        string time = DateTime.Now.ToString("hh_mm_ss");

        GDALUtils.RasterizeFeatureCollection(rates, $"geoms/raster_{time}.tiff");
        //Get task properties using ADAPT
        var field = task.FieldBoundaries();
        var zones = task.PrescriptionZones();


                
        //Serialize the zones to GeoJSON
        AdaptConverter.SaveJSON(zones, "geoms/zones.json");
        AdaptConverter.SaveJSON(field, "geoms/field.json");
        //Can be rasterized using gdal: gdal_rasterize -a rate -ot Int16 -ts 1000 1000 rates.json zones.tif
        AdaptConverter.SaveJSON(rates, "geoms/rates.json");

        MongoClient dbClient = new MongoClient();
        var db = dbClient.GetDatabase("TwinYields");
        db.DropCollection("Zones");
        var collection = db.GetCollection<MongoZone>("Zones");
        //var mz = new MongoZone("Test1", zones.First().Geometry);
        var bc = db.GetCollection<BsonDocument>("Zones");
        var json = AdaptConverter.ToJSON(zones.ElementAt(1));
        var doc = BsonDocument.Parse(json);
        bc.InsertOne(doc);
        
        var mz = new MongoZone("Test1", zones.ElementAt(1).Geometry);
        //zones.ElementAt(3)
        collection.InsertOne(mz);
        bc.InsertOne(BsonDocument.Parse(AdaptConverter.ToJSON(zones)));

        //var frame = task.PrescriptionFrame();
        //var idx = frame.IndexRowsUsing(r => (r.Get("rate0"), r.Get("rate1")));
        var operations = task.GroupOperations();

        Console.WriteLine("Done with taskfile");

        //Init database
        /*
        using var db = new TwinYields.DB.TwinYieldsContext();
        //Truncate at Init
        db.Database.ExecuteSqlRaw("TRUNCATE \"Farm\" cascade");
        db.SaveChanges();

        var smartfarm = new Farm { Name = "Luke Smart Farm" };
        db.Add(smartfarm);
        var dbfield = new Field { Farm = smartfarm, Geometry = field, Name = "RVIII" };
        db.Add(dbfield);

        foreach (var z in zones)
        {
            db.Add(new Zone
            {
                Field = dbfield,
                Geometry = z.Geometry,
                Crop = "Wheat",
            });
        }
        db.SaveChanges();*/

        Console.WriteLine("Done with DB");

        //Convert to APSIM simulations
        //Directory.Delete("simulations", true);
        //Directory.CreateDirectory("simulations");

        string protopath = @"prototypes/WheatProto.apsimx";
        string outName = "simulations/wheat_zones.apsimx";
        var sb = new APSIMBuilder();
        var simulations = sb.BuildSimulations(zones, protopath, outName);
        var simFiles = sb.BuildSimulationFiles(zones, protopath);
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

        var data = sims.FindChild<DataStore>();
        data.Open();
        var tables = data.Reader.TableNames;
        var dt = data.Reader.GetData(tables.First());
        Console.WriteLine(dt.Rows.Count);

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