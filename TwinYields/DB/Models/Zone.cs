using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace TwinYields.DB.Models
{
    public partial class Zone
    {
        public int Id { get; set; }
        public int? FieldId { get; set; }
        public Geometry Geometry { get; set; }
        public string Crop { get; set; }
        public string Cultivar { get; set; }

        public virtual Field Field { get; set; }
    }
}
