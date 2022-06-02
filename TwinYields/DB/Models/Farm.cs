using System;
using System.Collections.Generic;

namespace TwinYields.DB.Models
{
    public partial class Farm
    {
        public Farm()
        {
            Fields = new HashSet<Field>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Field> Fields { get; set; }
    }
}
