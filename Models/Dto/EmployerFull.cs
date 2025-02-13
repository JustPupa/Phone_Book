using System.Linq;

namespace Phone_book.Models.Dto
{
    public class EmployerFull
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
        public string Job { get; set; }
        public string? Home_Phone { get; set; }
        public string? Intern_Phone { get; set; }
        public string? Mobile_Phone { get; set; }
        public string? Email { get; set; }
        public bool? Fax { get; set; }
        public string Department { get; set; }
        public string? Subdepartment { get; set; }
        public string? Ops { get; set; }
        public static EmployerFull PhoneToEmp(Person phone)
        {
            EmployerFull emp = new()
            {
                Id = phone.id,
                Fullname = phone.fullname,
                Job = phone.job,
                Home_Phone = phone.home_phone,
                Intern_Phone = phone.intern_phone,
                Mobile_Phone = phone.mob_phone,
                Email = phone.email,
                Fax = phone.fax
            };
            if (phone.ops != null)
            {
                Ops op = Repo.GetSingle<Ops>("ops", Repo.GetEqCondition("id", phone.ops.ToString()));
                emp.Ops = op.name;
                if (op.dep != null) emp.Department = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", op.dep.ToString())).name;
                if (op.subdep != null) emp.Subdepartment = Repo.GetSingle<Subdepartments>("subdepartments", Repo.GetEqCondition("id", op.subdep.ToString())).name;
            }
            else if (phone.subdepartment != null)
            {
                Subdepartments sdep = Repo.GetSingle<Subdepartments>("subdepartments", Repo.GetEqCondition("id", phone.subdepartment.ToString()));
                emp.Subdepartment = sdep.name;
                int depId = sdep.depId;
                emp.Department = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", depId.ToString())).name;
            }
            else
            {
                emp.Department = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", phone.department.ToString())).name;
            }
            
            return emp;
        }

        public static List<EmployerFull> PhoneToEmp(IQueryable<Person> phone)
        {
            List<EmployerFull> output = new();
            foreach (var p in phone)
            {
                output.Add(PhoneToEmp(p));
            }
            return output.OrderBy(o => o.Department).ThenBy(o => o.Subdepartment).ThenBy(o => o.Ops).ToList();
        }

        public static List<EmployerFull> PhoneToEmp(List<Person> phone)
        {
            List<EmployerFull> output = new();
            foreach (var p in phone)
            {
                output.Add(PhoneToEmp(p));
            }
            return output.OrderBy(o => o.Department).ThenBy(o => o.Subdepartment).ThenBy(o => o.Ops).ToList();
        }
    }
}