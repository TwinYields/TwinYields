using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace TwinYields.DataBase;

public class TwinDataBase
{
    MongoClient dbClient;
    MongoDatabaseBase db;

    public TwinDataBase()
    {
        dbClient = new MongoClient();
        db = (MongoDatabaseBase)dbClient.GetDatabase("TwinYields");
    }

    //Drop all all collections 
    public void DropAll()
    {
        db.DropCollection("Fields");
        db.DropCollection("Farms");
        var collection = db.GetCollection<Field>("Fields");
        var idx1 = new CreateIndexModel<Field>(Builders<Field>.IndexKeys.Geo2DSphere("Location"));
        var idx2 = new CreateIndexModel<Field>(Builders<Field>.IndexKeys.Geo2DSphere("Zones.Location"));
        var iname = collection.Indexes.CreateOne(idx1);
        iname = collection.Indexes.CreateOne(idx2);
    }

    public void Insert(Field field)
    {
        var collection = db.GetCollection<Field>("Fields");
        collection.InsertOne(field);
    }

    public void Insert(Farm farm)
    {
        var collection = db.GetCollection<Farm>("Farms");
        collection.InsertOne(farm);
    }

    public Field FindField(string Name)
    {
        var collection = db.GetCollection<Field>("Fields");
        var docs = collection.Find(new BsonDocument { { "Name", Name } }).ToList();
        return docs[0];
    }

}

public static class GeoConverters{
    public static GeoJsonPolygon<GeoJson2DCoordinates> NtsPolygonToMongo(Polygon poly)
    {
        var gf = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
        int N = poly.Coordinates.Count();
        var mongocrds = new GeoJson2DCoordinates[N];
        for (int i = 0; i < N; i++)
        {
            mongocrds[i] = GeoJson.Position(poly.Coordinates[i].X, poly.Coordinates[i].Y);
        }
        var polygon = GeoJson.Polygon(mongocrds);
        return polygon;
    }
}

public class Farm
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public Farm(string Name)
    {
        this.Name = Name;
    }
}
public class Field
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public GeoJsonPoint<GeoJson2DCoordinates> Location { get; set; }
    public GeoJsonPolygon<GeoJson2DCoordinates> Boundary { get; set; }

    public List<Zone> Zones;
    public ObjectId FarmId { get;set;}

    public Field(string Name, NetTopologySuite.Geometries.Polygon boundary)
    {
        this.Name = Name;
        this.Boundary = GeoConverters.NtsPolygonToMongo(boundary);
        this.Location = GeoJson.Point(GeoJson.Position(boundary.Centroid.X, boundary.Centroid.Y));
    }
}
public class Zone
{
    //public ObjectId Id { get; set; }
    public string Name { get; set; }
    public double[] Rates { get; set; }
    public string[] Products { get; set; }
    public GeoJsonPoint<GeoJson2DCoordinates> Location { get; set; }
    public GeoJsonPolygon<GeoJson2DCoordinates> Boundary { get; set; }

    public Zone(string Name, double[] Rates, string[] Products, NetTopologySuite.Geometries.Polygon boundary)
    {
        this.Boundary = GeoConverters.NtsPolygonToMongo(boundary);
        this.Location = GeoJson.Point(GeoJson.Position(boundary.Centroid.X, boundary.Centroid.Y));
        this.Name = Name;
        this.Rates = Rates;
        this.Products = Products;
    }
}
