using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phone_book.Models
{
    public interface Identifiable
    {
        public int id { get; set; }
    }

    [Table("departments")]
    public class Departments : Identifiable
    {
        [Required]
        public int id { get; set; }
        public string name { get; set; }
    }

    [Table("subdepartments")]
    public class Subdepartments : Identifiable
    {
        [Required]
        public int id { get; set; }
        public string name { get; set; }
        public int depId { get; set; }
    }

    [Table("person")]
    public class Person : Identifiable
    {
        [Required]
        public int id { get; set; }
        [Required]
        public string fullname { get; set; }
        [Required]
        public string job { get; set; }
        public string? home_phone { get; set; }
        public string? intern_phone { get; set; }
        public string? mob_phone { get; set; }
        public int? department { get; set; }
        public string? email { get; set; }
        public bool? fax { get; set; }
        public int? subdepartment { get; set; }
        public int? ops { get; set; }

        //Equals & GetHashCode methods are necessary for Intersect usage while searching for multiple results
        public override bool Equals(object obj)
        {
            if (obj is not Person item)
            {
                return false;
            }

            return (id == item.id && fullname == item.fullname && job == item.job && home_phone == item.home_phone
                && intern_phone == item.intern_phone && mob_phone == item.mob_phone && department == item.department
                && email == item.email && fax == item.fax && subdepartment == item.subdepartment && ops == item.ops);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }

    [Table("ops")]
    public class Ops : Identifiable
    {
        [Required]
        public int id { get; set; }
        public string name { get; set; }
        public int? dep { get; set; }
        public int? subdep { get; set; }
        public int? index { get; set; }
        public string? address { get; set; }
    }
}
