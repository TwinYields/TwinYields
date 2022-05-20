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
using Models.Core;
using Models.Core.ApsimFile;

namespace TwinConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var importPath = @"C:\Users\03080535\TASKDATA_20210603_0159";
            var task = new AdaptConverter(importPath);
            
            var field = task.FieldBoundaries();
            var zones = task.PrescriptionZones();
            var frame = task.PrescriptionFrame();
            //var idx = frame.IndexRowsUsing(r => (r.Get("rate0"), r.Get("rate1")));

            string apsimxFilePath = @"prototypes/WheatProto.apsimx";
            Simulations file = FileFormat.ReadFromFile<Simulations>(apsimxFilePath, e => throw e, false);


            var serializer = GeoJsonSerializer.Create();
            string geoJson;
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, zones);
                geoJson = stringWriter.ToString();
            }
            File.WriteAllText("geoms/zones.json", geoJson);



            Console.WriteLine("Hello World!");

        }
    }
}
