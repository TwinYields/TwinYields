using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace TwinYields.DB.Models
{
    public partial class Field
    {
        public Field()
        {
            Zones = new HashSet<Zone>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public long LpisId { get; set; }
        public Geometry Geometry { get; set; }
        public int FarmId { get; set; }

        public virtual Farm Farm { get; set; }
        public virtual ICollection<Zone> Zones { get; set; }
    }
}
