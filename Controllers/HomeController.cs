using Phone_book.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Phone_book.Models.Dto;
using System.Net;

namespace Phone_book.Controllers
{
    public class HomeController : Controller
    {
        //List of highly dependent connections to avoid redundant creation of tables and queries to SQL server
        //Each item represents a regional postal hub or a large department to be viewed after selecting one of the main tabs
        public List<int> MogRupsIds = new() { 1, 2, 3, 4, 5, 6 };
        public List<int> OCPSIds = new() { 7, 8, 9 };

        //Start page with a lists of items for autocomplete w/o sql query
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 900, Location = ResponseCacheLocation.Client)]
        public IActionResult Index()
        {
            var fiojobs = Repo.GetAll<Person>("person").Select(p => (new IdName(p.id, p.job), new IdName(p.id, p.fullname)));
            var allops = Repo.GetAll<Ops>("ops");
            DSPidsNamesOnly dpsShort = new()
            {
                Deps = Repo.GetAll<Departments>("departments").Select(d => new IdName(d.id, d.name)).ToList(),
                SubDeps = Repo.GetAll<Subdepartments>("subdepartments").Select(s => new IdName(s.id, s.name)).ToList(),
                Ops = allops.Select(s => new IdName(s.id, s.name)).ToList(),
                Phones = fiojobs.Select(f => f.Item2).ToList(),
                Jobs = fiojobs.Select(f => f.Item1).ToList(),
                Indexes = allops.Select(s => new IdName(s.id, s.index.ToString())).ToList()
            };
            return View(dpsShort);
        }

        //Searching by multiple fields
        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 300, Location = ResponseCacheLocation.Client)]
        public JsonResult GetPhonesByMulti(string dep, string sdep, string ops, string phone, string job, 
            string numb, string inphone, string mob, string mail)
        {
            List<Person> phonesByDoubleIds = new();
            List<(string, string, SQLwhereOperations)> whereState = new();
            //The search is carried out not only by department field but also by all sub departments and post offices in its charge
            if (dep != null)
            {
                //We get a list of all departments like entered one
                List<int?> depIds = Repo.GetByWhere<Departments>("departments", Repo.GetLikeCondition("name", dep)).Select(d => d.id as int?).ToList();
                List<int?> sdepIds = new();
                foreach(var d in depIds)
                {
                    sdepIds.AddRange(Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetEqCondition("depId", d.ToString())).Select(s => s.id as int?).ToList());
                }
                List<int?> opsIds = new();
                //Then we get all post offices ids from the lists of found departments and sub departments
                foreach (var d in depIds)
                {
                    opsIds.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("dep", d.ToString())).Select(s => s.id as int?).ToList());
                }
                foreach (var s in sdepIds)
                {
                    opsIds.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", s.ToString())).Select(s => s.id as int?).ToList());
                }
                phonesByDoubleIds = Repo
                    .GetAll<Person>("person")
                    .Where(p =>
                        (depIds.Contains(p.department)) ||
                        (sdepIds.Contains(p.subdepartment)) ||
                        (opsIds.Contains(p.ops)))
                    .ToList();
            }
            //The same but easier one for sub department coincidences
            if (sdep != null)
            {
                List<int?> sdepIds = Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetLikeCondition("name", sdep)).Select(s => s.id as int?).ToList();
                List<int?> opsids = new();
                foreach (var s in sdepIds)
                {
                    opsids.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", s.ToString())).Select(s => s.id as int?).ToList());
                }
                if (dep != null)
                {
                    phonesByDoubleIds = phonesByDoubleIds.Where(pdi => sdepIds.Contains(pdi.subdepartment) || opsids.Contains(pdi.ops)).ToList();
                }
                else
                {
                    phonesByDoubleIds = Repo
                        .GetAll<Person>("person")
                        .Where(p =>
                            (sdepIds.Contains(p.subdepartment)) ||
                            (opsids.Contains(p.ops)))
                        .ToList();
                }
            }
            //The final one complex searching by post office coincidence
            if (ops != null)
            {
                var opsids = Repo.GetByWhere<Ops>("ops", Repo.GetLikeCondition("name", ops)).Select(o => o.id as int?).ToList();
                if (dep != null || sdep != null)
                {
                    phonesByDoubleIds = phonesByDoubleIds.Where(pdi => opsids.Contains(pdi.ops)).ToList();
                }
                else
                {
                    phonesByDoubleIds = Repo
                        .GetAll<Person>("person")
                        .Where(p => opsids.Contains(p.ops))
                        .ToList();
                }
            }
            //If any condition is satisfied it goes to multiple WHERE-OR query 
            if (!string.IsNullOrEmpty(phone))
            {
                whereState.Add(("fullname", phone, SQLwhereOperations.Contains));
            }
            if (!string.IsNullOrEmpty(job))
            {
                whereState.Add(("job", job, SQLwhereOperations.Contains));
            }
            if (!string.IsNullOrEmpty(numb))
            {
                whereState.Add(("home_phone", numb, SQLwhereOperations.Contains));
            }
            if (!string.IsNullOrEmpty(inphone))
            {
                whereState.Add(("intern_phone", inphone, SQLwhereOperations.Contains));
            }
            if (!string.IsNullOrEmpty(mob))
            {
                whereState.Add(("mob_phone", mob, SQLwhereOperations.Contains));
            }
            if (!string.IsNullOrEmpty(mail))
            {
                whereState.Add(("email", mail, SQLwhereOperations.Contains));
            }
            //Output only by one-field searching
            List<Person> remainingFilterResult = Repo.GetByWhere<Person>("person", whereState).ToList();
            
            if (dep != null || sdep != null || ops != null)
            {
                //If there are two sets of results by department, post office etc. & one-field search
                //We should take only those results that satisfy both conditions
                if (phonesByDoubleIds.Any() && remainingFilterResult.Any() && remainingFilterResult.Intersect(phonesByDoubleIds).Any())
                {
                    return Json(EmployerFull.PhoneToEmp(remainingFilterResult.Intersect(phonesByDoubleIds).ToList()));
                }
                //If there is a match only on the first criterion
                else if (phonesByDoubleIds.Any() && !remainingFilterResult.Any())
                {
                    return Json(EmployerFull.PhoneToEmp(phonesByDoubleIds));
                }
            }
            //In case if the only matches are by one-field searching
            if (remainingFilterResult.Any())
            {
                return Json(EmployerFull.PhoneToEmp(remainingFilterResult));
            }
            //If there are no matches  both searches
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            string responseText = "Ничего не найдено по запросу";
            return Json(responseText);
        }

        //One of the six structural units has been selected
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 900, Location = ResponseCacheLocation.Client)]
        [HttpGet]
        public IActionResult RupsSelected(string rup, int? selTabInd = null)
        {
            var rwu = GetRwu(rup);
            rwu.SelTabInd = selTabInd;
            //Different view for a management tab
            if (rup == "Mog")
            {
                return View("Management", rwu);
            }
            //Standart view for another structural unit
            return View("RupsConcrete", rwu);
        }

        //Sub department view method 
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 900, Location = ResponseCacheLocation.Client)]
        [HttpGet]
        public IActionResult UPSselected(int ups, string alias, int? selTabInd=null)
        {
            //Show all post offices and employees related to selected sub department
            Subdepartments upsRepo = Repo.GetSingle<Subdepartments>("subdepartments", Repo.GetEqCondition("id", ups.ToString()));
            List<Ops> opses = Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", ups.ToString())).ToList();
            List<Person> phns = Repo.GetByWhere<Person>("person", Repo.GetEqCondition("subdepartment", ups.ToString())).ToList();
            UpsInfo upsinf = new()
            {
                Id = ups,
                DepId = upsRepo.depId,
                UpsName = upsRepo.name,
                Opses = new(),
                Employees = new(),
                DepName = alias
            };
            if (opses.Any())
            {
                upsinf.Opses = opses;
            }
            if (phns.Any())
            {
                upsinf.Employees = phns;
            }
            //selTabInd parameter indicates which tab was clicked previously for faster redirection if needed
            upsinf.SelTabInd = selTabInd;
            return View("UpsInfoView", upsinf);
        }

        //Collect all department data using its alias
        public RupsWithUps GetRwu(string rup)
        {
            string rupName = "Неизвестно";
            List<(string name, List<Subdepartments> phones)> sdeps = new();
            List<(string name, List<Person> phones)> emps = new();
            List<Ops> Opses = new();
            switch (rup)
            {
                case "Mog":
                    {
                        rupName = "Могилёвский филиал РУП \"Белпочта\"";
                        foreach (var item in GetDepsById(MogRupsIds))
                        {
                            SetSubsAndEmps(item);
                        }
                        break;
                    }
                case "Bob":
                    {
                        rupName = "Бобруйский региональный узел почтовой связи";
                        SetSubsAndEmps(GetDepsById("10"));
                        Opses = OpsByDepId("10");
                        break;
                    }
                case "Kri":
                    {
                        rupName = "Кричевский региональный узел почтовой связи";
                        SetSubsAndEmps(GetDepsById("11"));
                        Opses = OpsByDepId("11");
                        break;
                    }
                case "Gor":
                    {
                        rupName = "Горецкий региональный узел почтовой связи";
                        SetSubsAndEmps(GetDepsById("12"));
                        Opses = OpsByDepId("12");
                        break;
                    }
                case "OCPS":
                    {
                        rupName = "Объединённый цех почтовой связи";
                        foreach (var item in GetDepsById(OCPSIds))
                        {
                            SetSubsAndEmps(item);
                        }
                        Opses = OCPSIds.SelectMany(id => Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("dep", id.ToString()))).ToList();
                        break;
                    }
                case "MCPS":
                    {
                        rupName = "Могилёвский цех почтовой связи";
                        var depsById = GetDepsById("13");
                        Opses = OpsByDepId("13");
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return new()
            {
                RupsName = rupName,
                AliasName = rup,
                Upss = sdeps,
                EmployersWRups = emps,
                Opss = Opses
            };

            void SetSubsAndEmps(Departments deps)
            {
                sdeps.Add((deps.name, Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetEqCondition("depId", deps.id.ToString())).ToList()));
                emps.Add((deps.name, Repo.GetByWhere<Person>("person", Repo.GetEqCondition("department", deps.id.ToString())).ToList()));
            }
        }

        private static Departments GetDepsById(string id)
        {
            return Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("id", id));
        }

        private static IEnumerable<Departments> GetDepsById(List<int> ids)
        {
            return ids.SelectMany(id => Repo.GetByWhere<Departments>("departments", Repo.GetEqCondition("id", id.ToString())));
        }

        private static List<Ops> OpsByDepId(string depid)
        {
            return Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("dep", depid)).ToList();
        }

        //Gather all related data for autocomplete
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 300, Location = ResponseCacheLocation.Client)]
        [HttpGet]
        public JsonResult GetAutocompleteData(string input)
        {
            //In case we are searching for an employee
            var fios = GetPersonResults("fullname").Select(p => p.fullname).ToList();
            var jobs = GetPersonResults("job").Select(p => p.job).ToList();
            var phs = GetPersonResults("home_phone").Select(p => p.home_phone).ToList();
            var iphs = GetPersonResults("intern_phone").Select(p => p.intern_phone).ToList();
            var mpsh = GetPersonResults("mob_phone").Select(p => p.mob_phone).ToList();
            var emls = GetPersonResults("email").Select(p => p.email).ToList();

            //In case we are searching for a post office
            var deps = GetCustomTableResults<Departments>("departments", "name").Select(i => i.name).ToList();
            var sdeps = GetCustomTableResults<Subdepartments>("subdepartments", "name").Select(i => i.name).ToList();
            var opsnames = GetCustomTableResults<Ops>("ops", "name").Select(i => i.name).ToList();
            var indexes = GetCustomTableResults<Ops>("ops", "index").Select(i => i.index.ToString()).ToList();
            var addresses = GetCustomTableResults<Ops>("ops", "address").Select(i => i.address).ToList();

            var result = fios.Concat(jobs.Concat(phs.Concat(iphs.Concat(mpsh.Concat(emls.Concat
                (deps.Concat(sdeps.Concat(opsnames.Concat(indexes.Concat(addresses
            )))))))))).Distinct();

            if (!result.Any())
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                string responseText = "Ничего не найдено по запросу";
                return Json(responseText);
            }

            return Json(result.ToList());

            //Both methods are local because they are using an input parameter
            IQueryable<Person> GetPersonResults(string parameter)
            {
                return Repo.GetByWhere<Person>(
                    "person",
                    new List<(string propSql, string valSql, SQLwhereOperations sqlOp)> {
                    (parameter, input, SQLwhereOperations.Contains)
                });
            }

            IQueryable<T> GetCustomTableResults<T>(string tableName, string colName) where T : class, new()
            {
                return Repo.GetByWhere<T>(
                    tableName,
                    new List<(string propSql, string valSql, SQLwhereOperations sqlOp)> {
                    (colName, input, SQLwhereOperations.Contains)
                });
            }
        }

        //Global search by the only input
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 300, Location = ResponseCacheLocation.Client)]
        [HttpGet]
        public JsonResult GetPhonesByGlobal(string input)
        {
            List<(string, string, SQLwhereOperations)> whereCond = new()
            {
                ("fullname", input, SQLwhereOperations.Contains),
                ("job", input, SQLwhereOperations.Contains),
                ("home_phone", input, SQLwhereOperations.Contains),
                ("intern_phone", input, SQLwhereOperations.Contains),
                ("mob_phone", input, SQLwhereOperations.Contains),
                ("email", input, SQLwhereOperations.Contains)
            };
            //Get a preliminary result by simple fields
            var result = Repo.GetByWhere<Person>("person", whereCond, true);

            //Search if there are any people related to the input and write it to resultPerson parameter
            var deps = Repo.GetByWhere<Departments>(
                 "departments",
                 new List<(string propSql, string valSql, SQLwhereOperations sqlOp)> {
                    ("name", input, SQLwhereOperations.Contains)
             });

            var sdeps = Repo.GetByWhere<Subdepartments>(
                "subdepartments",
                new List<(string propSql, string valSql, SQLwhereOperations sqlOp)> {
                    ("name", input, SQLwhereOperations.Contains)
            });

            {
                var opss = Repo.GetByWhere<Ops>(
                    "ops",
                    new List<(string propSql, string valSql, SQLwhereOperations sqlOp)> {
                    ("name", input, SQLwhereOperations.Contains)
                });
                var resultPerson = new List<Person>();
                if (deps.Any())
                {
                    foreach(var d in deps)
                    {
                        var sdd = Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetEqCondition("depId", d.id.ToString()));
                        var opd = Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("dep", d.id.ToString()));
                        resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("department", d.id.ToString())));
                        foreach (var item in sdd)
                        {
                            resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("subdepartment", item.id.ToString())));
                        }
                        foreach (var item in opd)
                        {
                            resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("ops", item.id.ToString())));
                        }
                    }
                }
                if (sdeps.Any())
                {
                    foreach(var s in sdeps)
                    {
                        var opd = Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", s.id.ToString()));
                        resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("subdepartment", s.id.ToString())));
                        foreach (var item in opd)
                        {
                            resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("ops", item.id.ToString())));
                        }
                    }
                }
                if (opss.Any())
                {
                    foreach(var o in opss)
                    {
                        resultPerson.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("ops", o.id.ToString())));
                    }
                }
                if (resultPerson.Any())
                {
                    result = result.Concat(resultPerson).Distinct();
                }
            }

            //Search if there are any post offices related to the input and write it to resultOps parameter
            List<(string, string, SQLwhereOperations)> whereCondOps = new()
            {
                ("name", input, SQLwhereOperations.Contains),
                ("index", input, SQLwhereOperations.Contains),
                ("address", input, SQLwhereOperations.Contains)
            };
            var resultOps = Repo.GetByWhere<Ops>("ops", whereCondOps, true);
            var resultopsDep = new List<Ops>();
            {
                if (deps.Any())
                {
                    foreach (var d in deps)
                    {
                        var sdd = Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetEqCondition("depId", d.id.ToString()));
                        resultopsDep.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("dep", d.id.ToString())));
                        foreach (var item in sdd)
                        {
                            resultopsDep.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", item.id.ToString())));
                        }
                    }
                }
                if (sdeps.Any())
                {
                    foreach (var s in sdeps)
                    {
                        resultopsDep.AddRange(Repo.GetByWhere<Ops>("ops", Repo.GetEqCondition("subdep", s.id.ToString())));
                    }
                }
                if (resultopsDep.Any())
                {                      
                    resultOps = resultOps.Concat(resultopsDep).Distinct();
                }
            }
            //The final output consists of both lists of persons and post offices
            PhonesPlusOps finalResult = new() { 
                Emps = EmployerFull.PhoneToEmp(result),
                Opses = OpsInfoFull.ConvertToFull(resultOps) 
            };

            if (finalResult.Emps == null && finalResult.Opses == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                string responseText = "Ничего не найдено по запросу";
                return Json(responseText);
            }
            return Json(finalResult);
        }

        //Post office click action
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 900, Location = ResponseCacheLocation.Client)]
        [HttpGet]
        public IActionResult ViewOps(int opsId, string alias, int? selTabInd = null)
        {
            var phones = Repo.GetByWhere<Person>("person", Repo.GetEqCondition("ops", opsId.ToString())).ToList();
            var ops = Repo.GetSingle<Ops>("ops", Repo.GetEqCondition("id", opsId.ToString()));
            return View("PersonViewer", new EmployeesWithLink()
            {
                Employees = phones,
                Ops = (opsId, ops.name),
                DepAlias = alias,
                SelTabInd = selTabInd,
                Dep = (ops.dep, ""),
                Sdep = (ops.subdep, "")
            });
        }

        //Get management employees by the department for a drop down list
        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 900, Location = ResponseCacheLocation.Client)]
        public JsonResult GetManagersDropDown(string depName)
        {
            var depId = Repo.GetSingle<Departments>("departments", Repo.GetEqCondition("name", depName)).id;
            var sdeps = Repo.GetByWhere<Subdepartments>("subdepartments", Repo.GetEqCondition("depId", depId.ToString()));
            List<Person> output = new();
            foreach (var s in sdeps)
            {
                output.AddRange(Repo.GetByWhere<Person>("person", Repo.GetEqCondition("subdepartment", s.id.ToString())));
            }
            var grpby = output
                .GroupBy(o => o.subdepartment)
                .ToList()
                .Select(o => new SdepPhonesGroup(
                    Repo.GetSingle<Subdepartments>("subdepartments", Repo.GetEqCondition("id", o.Key.ToString())).name,
                    o.ToList()));
            return Json(grpby);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}