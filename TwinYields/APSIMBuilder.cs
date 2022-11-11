using System.Collections.Generic;
using System.IO;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Climate;

namespace TwinYields;

public class APSIMBuilder
{

    //Build APSIM Simulations in single .apsimx file for a field based on zones extracted from Taskfile
    public Simulations BuildSimulations(List<DataBase.Zone> zones, string prototype, string outName)
    {
        Simulations sims = FileFormat.ReadFromFile<Simulations>(prototype, e => throw e, false);
        //Get original simulation from prototype file
        var simulation = sims.FindChild<Simulation>();

        //Clone and modify simulation to match zone features
        int zoneidx = 0;
        foreach (var zone in zones)
        {
            simulation.FileName = outName;
            sims.FindChild<Models.Storage.DataStore>().FileName = outName;
            //Change timing
            var clock = simulation.FindChild<Clock>();
            clock.StartDate = new System.DateTime(2022, 04, 01, 0, 0, 0);
            //clock.StartDate = new System.DateTime(1985, 05, 01, 0, 0, 0);
            //clock.EndDate = System.DateTime.Today.AddDays(-1);
            clock.EndDate = new System.DateTime(2022, 10, 01, 0, 0, 0);

            //Modify management actions
            var simField = simulation.FindChild<Zone>();
            var managementActions = simField.FindAllChildren<Manager>();
            //var fertilizer_kg = (double)zone.Attributes["rate"];
            var fertilizer_kg = zone.Rates[0];
            foreach (var action in managementActions)
            {
                switch (action.Name)
                {
                    case "SowingFertilizerNO3":
                        //TODO get fertilizer specific values from DB
                        //https://www.yara.fi/lannoitus/lannoitteet/yarabela/yarabela-suomensalpietari/
                        var NO3kg = 0.122 * fertilizer_kg;
                        action.Parameters[2] = new KeyValuePair<string, string>("Amount", NO3kg.ToString());
                        break;
                    case "SowingFertilizerNH4":
                        var NH4kg = 0.146 * fertilizer_kg;
                        action.Parameters[2] = new KeyValuePair<string, string>("Amount", NH4kg.ToString());
                        break;
                    case "Sow on a fixed date":
                        action.Parameters[1] = new KeyValuePair<string, string>("SowDate", "23-May");
                        action.Parameters[2] = new KeyValuePair<string, string>("CultivarName", "WheatJarli");
                        break;
                    default:
                        break;
                }

                //Console.WriteLine(action.Name);
            }

            simField.Name = $"zone_{zoneidx}";
            var weather = simulation.FindChild<Weather>();
            //weather.FileName = @"..\weatherfiles\Dalby.met";
            weather.FileName = @"..\weatherfiles\Jokioinen.met";
            //weather.FileName = @"..\weatherfiles\Jokioinen_1985.met";

            var newSim = simulation.Clone();
            newSim.Name = $"zone_{zoneidx}";
            newSim.FileName = outName;
            sims.Children.Add(newSim);
            zoneidx++;
        }

        //Remove the unmodified simulation
        sims.Children.Remove(simulation);
        var json = FileFormat.WriteToString(sims);
        File.WriteAllText(outName, json);

        return sims;
    }

    //Build APSIM Simulations as separate .apsimx file for a field based on zones extracted from Taskfile
    public List<string> BuildSimulationFiles(List<DataBase.Zone> zones, string prototype, string outPath="simulations/wheat_")
    {
        Simulations sims = FileFormat.ReadFromFile<Simulations>(prototype, e => throw e, false);

        string json;
        string outName;
        var simFiles = new List<string>();
        Simulation simulation;
        foreach (var zone in zones)
        {
            outName = outPath  + zone.Rates[0] + ".apsimx";
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
                        action.Parameters[0] = new KeyValuePair<string, string>("Amount", zone.Rates[0].ToString()); //TODO check units and match fertilizer types
                        break;
                    case "Sow on a fixed date":
                        action.Parameters[1] = new KeyValuePair<string, string>("SowDate", "23-May");
                        break;
                    default:
                        break;
                }
            }
            //Using CustomFileName like this leads to no db output
            //sims.FindChild<Models.Storage.DataStore>().CustomFileName = "simulations/db/wheat_" + zone.Attributes["rate"] + ".apsimx";
            sims.FileName = outName;
            json = FileFormat.WriteToString(sims);
            File.WriteAllText(outName, json);
            simFiles.Add(outName);
        }

        return simFiles;
    }


}