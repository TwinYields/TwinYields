using System;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Prescriptions;
using NetTopologySuite;
using NetTopologySuite.Features;
using Deedle;
using NetTopologySuite.IO;
using System.IO;
using Newtonsoft.Json;

namespace TwinYields;

public class AdaptConverter
{
    public ApplicationDataModel dataModel;
    private GeometryFactory gf = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
    public AdaptConverter(ApplicationDataModel dataModel)
    {
        this.dataModel = dataModel;
    }
    public AdaptConverter(string importPath)
    {
        var isoxmlPlugin = new AgGateway.ADAPT.ISOv4Plugin.Plugin();
        this.dataModel = isoxmlPlugin.Import(importPath)[0];
    }
    public Polygon FieldBoundaries()
    {
        var bounds = this.dataModel.Catalog.FieldBoundaries.First().SpatialData.Polygons.First();
        var fieldCoords = new List<Coordinate>();
        foreach (var pnt in bounds.ExteriorRing.Points)
            fieldCoords.Add(new Coordinate(pnt.X, pnt.Y));
        var field = gf.CreatePolygon(fieldCoords.ToArray());
        //field.UserData = "Jokioinen";
        return field;
    }
    public FeatureCollection VectorizePrescription()
    {
        var field = this.FieldBoundaries();
        var p = (RasterGridPrescription)dataModel.Catalog.Prescriptions.First();

        var x0 = p.Origin.X;
        var y0 = p.Origin.Y;
        var sx = p.CellWidth.Value.Value;
        var sy = p.CellHeight.Value.Value;
        int r = 0;
        int c = 0;
        var N = p.Rates.Count();
        var rates = new FeatureCollection();
        //Convert from raster to coordinates
        for (int i = 0; i < N; i++)
        {
            var x1 = x0 + c * sx;
            var y1 = y0 + r * sy;
            var x2 = x0 + (c + 1) * sx;
            var y2 = y0 + (r + 1) * sy;

            var poly = gf.CreatePolygon(new[] {
                new Coordinate(x1, y1),
                new Coordinate(x1, y2),
                new Coordinate(x2, y2),
                new Coordinate(x2, y1),
                new Coordinate(x1, y1),
            });

            if (field.Contains(poly))
            {
                var att = new AttributesTable();
                for (int ridx = 0; ridx < p.Rates[i].RxRates.Count; ridx++)
                {
                    var rate = p.Rates[i].RxRates[ridx].Rate;
                    att.Add("rate" + ridx.ToString(), rate);
                }
                rates.Add(new Feature(poly, att));
            }

            if (c == p.ColumnCount - 1)
            {
                c = 0;
                r++;
            }
            else
            {
                c++;
            }
        }

        return rates;
    }
    public FeatureCollection PrescriptionZones()
    {
        var field = this.FieldBoundaries();
        var p = (RasterGridPrescription)dataModel.Catalog.Prescriptions.First();
        var uniqueRates = p.Rates.Select(x => x.RxRates[0].Rate).Distinct();

        var zones = new Dictionary<double, List<Polygon>>();
        foreach (var rate in uniqueRates)
            zones.Add(rate, new List<Polygon>());

        //var zones = new List<Polygon>[uniqueRates.Count];
        //for (int i = 0; i < uniqueRates.Count; i++)
        //    zones[i] = new List<Polygon>();

        var x0 = p.Origin.X;
        var y0 = p.Origin.Y;
        var sx = p.CellWidth.Value.Value;
        var sy = p.CellHeight.Value.Value;
        int r = 0;
        int c = 0;
        var N = p.Rates.Count();
        //Convert from raster to coordinates
        for (int i = 0; i < N; i++)
        {
            var x1 = x0 + c * sx;
            var y1 = y0 + r * sy;
            var x2 = x0 + (c + 1) * sx;
            var y2 = y0 + (r + 1) * sy;

            var poly = gf.CreatePolygon(new[] {
                new Coordinate(x1, y1),
                new Coordinate(x1, y2),
                new Coordinate(x2, y2),
                new Coordinate(x2, y1),
                new Coordinate(x1, y1),
            });

            if (field.Contains(poly))
            {
                //var uidx = uniqueRates.FindIndex(0, uniqueRates.Count, x => x == p.Rates[i].RxRates[0].Rate);
                //zones[uidx].Add(poly);
                zones[p.Rates[i].RxRates[0].Rate].Add(poly);
            }

            if (c == p.ColumnCount - 1)
            {
                c = 0;
                r++;
            }
            else
            {
                c++;
            }
        }

        //Simplify zones and build featurecollection
        var features = new FeatureCollection();
        foreach (var rate in zones.Keys)
        {
            var att = new AttributesTable();
            att.Add("rate", rate);
            var geom = gf.CreateGeometryCollection(zones[rate].ToArray()).Union();
            features.Add(new Feature(geom, att));
        }

        return features;
    }
    public List<List<OperationData>> GroupOperations()
    {
        IEnumerable<OperationData> operationData = dataModel.Documents.LoggedData.First().OperationData;
        var handled = new List<int>();
        var groupedData = new List<List<OperationData>>();
        foreach (var opdata in operationData)
        {
            if (handled.Contains(opdata.Id.ReferenceId))
            {
                continue;
            }
            else
            {
                var operations = new List<OperationData>();
                operations.Add(opdata);
                var r0 = operationData.Where(x => x.CoincidentOperationDataIds.Contains(opdata.Id.ReferenceId)).ToList();
                operations.AddRange(r0);
                handled.AddRange(opdata.CoincidentOperationDataIds);
                groupedData.Add(operations);
            }
        }

        return groupedData;
    }
    public Frame<int,  String> PrescriptionFrame()
    {
        var field = this.FieldBoundaries();
        var p = (RasterGridPrescription)dataModel.Catalog.Prescriptions.First();

        var x0 = p.Origin.X;
        var y0 = p.Origin.Y;
        var sx = p.CellWidth.Value.Value;
        var sy = p.CellHeight.Value.Value;
        int r = 0;
        int c = 0;
        var N = p.Rates.Count();

        var rows = new List<KeyValuePair<int, Series<string, object>>>();
        //Convert from raster to coordinates
        for (int i = 0; i < N; i++)
        {
            var x1 = x0 + c * sx;
            var y1 = y0 + r * sy;
            var x2 = x0 + (c + 1) * sx;
            var y2 = y0 + (r + 1) * sy;

            var poly = gf.CreatePolygon(new[] {
                new Coordinate(x1, y1),
                new Coordinate(x1, y2),
                new Coordinate(x2, y2),
                new Coordinate(x2, y1),
                new Coordinate(x1, y1),
            });

            if (field.Contains(poly))
            {
                var sb = new SeriesBuilder<string>();
                sb.Add("geom", poly);
                int ridx = 0;
                foreach (var rate in p.Rates[i].RxRates)
                {
                    sb.Add("rate" + ridx, rate.Rate);
                    ridx++;
                }
                var series = KeyValue.Create(i, sb.Series);
                rows.Add(series);
            }

            if (c == p.ColumnCount - 1)
            {
                c = 0;
                r++;
            }
            else
            {
                c++;
            }
        }

        //var rowsB = Enumerable.Range(0, 100).Select(i => {
        // Build each row using series builder & return
        // KeyValue representing row key with row data
        //     var sb = new SeriesBuilder<string>();
        //     sb.Add("Index", i);
        //     sb.Add("Sin", Math.Sin(i / 100.0));
        //     sb.Add("Cos", Math.Cos(i / 100.0));
        //     return KeyValue.Create(i, sb.Series);
        // });

        return Frame.FromRows(rows);
        // Turn sequence of row information into data frame


        //var frame = Frame.FromRows(rows);

    }

    public void SaveJSON(Polygon features, string fileName)
    {
        var serializer = GeoJsonSerializer.Create();
        string geoJson;
        using (var stringWriter = new StringWriter())
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            serializer.Serialize(jsonWriter, features);
            geoJson = stringWriter.ToString();
        }
        File.WriteAllText(fileName, geoJson);
    }

    public void SaveJSON(FeatureCollection features, string fileName)
    {
        var serializer = GeoJsonSerializer.Create();
        string geoJson;
        using (var stringWriter = new StringWriter())
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            serializer.Serialize(jsonWriter, features);
            geoJson = stringWriter.ToString();
        }
        File.WriteAllText(fileName, geoJson);
    }



}