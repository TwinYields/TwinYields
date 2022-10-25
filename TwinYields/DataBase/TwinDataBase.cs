using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace TwinYields.DataBase;

public class TwinDataBase
{
    MongoClient dbClient;
    public MongoDatabaseBase db;

    public TwinDataBase()
    {
        dbClient = new MongoClient();
        db = (MongoDatabaseBase)dbClient.GetDatabase("TwinYields");
        var pack = new ConventionPack();
        pack.AddMemberMapConvention(
            "LowerCaseElementName",
            m => m.SetElementName(m.MemberName.ToLower()));
        ConventionRegistry.Register("TwinYields conventions",
            pack,
            t => true
            );
    }

    //Drop all all collections 
    public void DropAll()
    {
        db.DropCollection("Fields");
        db.DropCollection("Farms");
        db.DropCollection("SimulationFiles");
        var collection = db.GetCollection<Field>("Fields");
        var idx1 = new CreateIndexModel<Field>(Builders<Field>.IndexKeys.Geo2DSphere("Location"));
        var idx2 = new CreateIndexModel<Field>(Builders<Field>.IndexKeys.Geo2DSphere("Zones.Location"));
        var iname = collection.Indexes.CreateOne(idx1);
        iname = collection.Indexes.CreateOne(idx2);
    }

    public void DropSimulationFiles()
    {
        db.DropCollection("SimulationFiles");
    }

    public void Insert(Field field)
    {
        this.Insert(field, "Fields");
    }

    public void Insert(Farm farm)
    {
        this.Insert(farm, "Farms");
    }

    public void Insert(SimulationFile simFile)
    {
        this.Insert(simFile, "SimulationFiles");
    }

    public void Insert<T>(T doc, string collection)
    {
        var col = db.GetCollection<T>(collection);
        col.InsertOne(doc);
    }

    public List<Field> FindFields()
    {
        var collection = db.GetCollection<Field>("Fields");
        var docs = collection.Find(new BsonDocument {}).ToList();
        return docs;
    }

    public Field FindField(string Name)
    {
        var collection = db.GetCollection<Field>("Fields");
        var docs = collection.Find(new BsonDocument { { "name", Name } }).ToList();
        return docs[0];
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
    public GeoJsonPolygon<GeoJson2DCoordinates> Geometry { get; set; }

    public List<Zone> Zones;
    public ObjectId FarmId { get;set;}

}

public class Zone
{
    //public ObjectId Id { get; set; }
    public string Name { get; set; }
    public double[] Rates { get; set; }
    public string[] Products { get; set; }
    public GeoJsonPoint<GeoJson2DCoordinates> Location { get; set; }
    public GeoJsonPolygon<GeoJson2DCoordinates> Geometry { get; set; }
}

public class SimulationFile
{
    public ObjectId Id { get; set; }
    public string Field { get; set; }
    public string Path { get; set; }

    public SimulationFile(string field, string path)
    {
        Field = field;
        Path = path;
    }
}
