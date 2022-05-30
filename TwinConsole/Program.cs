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

namespace TwinConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var importPath = "TASKDATA_20210603_0159";
            var task = new AdaptConverter(importPath);
            
            var field = task.FieldBoundaries();
            var zones = task.PrescriptionZones();
            //var frame = task.PrescriptionFrame();
            //var idx = frame.IndexRowsUsing(r => (r.Get("rate0"), r.Get("rate1")));
            var operations = task.GroupOperations();

            string apsimxFilePath = @"prototypes/WheatProto.apsimx";
            Simulations sims = FileFormat.ReadFromFile<Simulations>(apsimxFilePath, e => throw e, false);
            Directory.Delete("simulations", true);
            Directory.CreateDirectory("simulations");
            //Directory.CreateDirectory("simulations/db");

            //Simulations newSim;

            //Generate simulations from Task files
            //TODO get dates from Task file
            string outName;
            string json;
            var simFiles = new List<string>();
            Simulation simulation;
            foreach (var zone in zones)
            {
                outName = "simulations/wheat_" + zone.Attributes["rate"] + ".apsimx";
                sims.FindChild<Simulation>().FileName = outName;
                sims.FindChild<Models.Storage.DataStore>().FileName = outName;
                simulation = sims.FindChild<Simulation>();

                //Change timing
                var clock = simulation.FindChild<Clock>();
                //clock.StartDate = new System.DateTime(2022, 5, 15, 0, 0, 0);
                //clock.EndDate = System.DateTime.Now;

                //Modify management actions
                var simField = simulation.FindChild<Zone>();
                var managementActions = simField.FindAllChildren<Manager>();
                foreach (var action in managementActions)
                {
                    switch (action.Name)
                    {
                        case "SowingFertiliser":
                            action.Parameters[0] = new KeyValuePair<string, string>("Amount", zone.Attributes["rate"].ToString()); //TODO check units and match fertilizer types
                            break;
                        case "Sow on a fixed date":
                            action.Parameters[1] = new KeyValuePair<string, string>("SowDate", "23-May");
                            break;
                        default:
                            break;
                    }

                    //Console.WriteLine(action.Name);
                }

                //Using CustomFileName like this leads to no db output
                //sims.FindChild<Models.Storage.DataStore>().CustomFileName = "simulations/db/wheat_" + zone.Attributes["rate"] + ".apsimx";
                sims.FileName = outName;
                json = FileFormat.WriteToString(sims);
                File.WriteAllText(outName, json);
                simFiles.Add(outName);
            }

            // Use the same file, but different simulations
            outName = "simulations/wheat_zones.apsimx";
            simulation = sims.FindChild<Simulation>();
            foreach (var zone in zones)
            {
                simulation.FileName = outName;
                sims.FindChild<Models.Storage.DataStore>().FileName = outName;
                //Change timing
                var clock = simulation.FindChild<Clock>();
                //clock.StartDate = new System.DateTime(2022, 5, 15, 0, 0, 0);
                //clock.EndDate = System.DateTime.Now;

                //Modify management actions
                var simField = simulation.FindChild<Zone>();
                var managementActions = simField.FindAllChildren<Manager>();
                foreach (var action in managementActions)
                {
                    switch (action.Name)
                    {
                        case "SowingFertiliser":
                            action.Parameters[0] = new KeyValuePair<string, string>("Amount", zone.Attributes["rate"].ToString()); //TODO check units and match fertilizer types
                            break;
                        case "Sow on a fixed date":
                            action.Parameters[1] = new KeyValuePair<string, string>("SowDate", "23-May");
                            break;
                        default:
                            break;
                    }

                    //Console.WriteLine(action.Name);
                }
                
                simField.Name = "zone_" + zone.Attributes["rate"];
                var newSim = simulation.Clone();
                newSim.Name = "zone_" + zone.Attributes["rate"];
                newSim.FileName = outName;
                sims.Children.Add(newSim);
            }

            sims.Children.Remove(simulation);
            sims.FileName = outName;
            json = FileFormat.WriteToString(sims);
            File.WriteAllText(outName, json);
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
}
