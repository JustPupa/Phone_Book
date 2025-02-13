using FastMember;
using Microsoft.EntityFrameworkCore;
using System.Data.Odbc;
using System.Reflection;
using Phone_book.Models;

namespace Phone_book
{
    public static class Repo
    {
        private static IRepository Rep { get; set; }
        static Repo()
        {
            try
            {
                var a = new TASQLRepository().GetAll<Departments>("departments");
                Rep = new TASQLRepository();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось выполнить подключение с помощью DbContext.\n" +
                    "Будет использован ODBC драйвер по умолчанию.\nСообщение:\n" + ex.Message);
                Rep = new ODBCRepository();
            }
        }
        public static string GetStrategyString()
        {
            return (Rep is ODBCRepository) ? "ODBC" : "SQL";
        }
        public static void SetStrategy(IRepository repository)
        {
            Rep = repository;
        }
        public static List<T> GetAll<T>(string instanceName) where T : new()
        {
            return Rep.GetAll<T>(instanceName);
        }
        public static T GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            return Rep.GetSingle<T>(instanceName, whereConditions, isOr);
        }
        public static IQueryable<T> GetByWhere<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            return Rep.GetByWhere<T>(instanceName, whereConditions, isOr);
        }
        public static void Insert<T>(string instanceName, T instance) where T : class
        {
            Rep.Insert(instanceName, instance);
        }
        public static void Remove<T>(string instanceName, T instance) where T : class
        {
            Rep.Remove(instanceName, instance);
        }
        public static void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            Rep.Update<T, I>(instance, whereConditions, valuesToUpd);
        }
        public static List<(string, string, SQLwhereOperations)> GetEqCondition(string s1, string s2)
        {
            return new List<(string, string, SQLwhereOperations)> { (s1, s2, SQLwhereOperations.Equals) };
        }
        public static List<(string, string, SQLwhereOperations)> GetLikeCondition(string s1, string s2)
        {
            return new List<(string, string, SQLwhereOperations)> { (s1, s2, SQLwhereOperations.Contains) };
        }
        public static List<(string, string, SQLwhereOperations)> GetEqCondition((string s1, string s2) p1, (string s3, string s4) p2)
        {
            return GetEqCondition(p1.s1, p1.s2)
                .Concat(GetEqCondition(p2.s3, p2.s4))
                .ToList();
        }
        public static List<(string, string, SQLwhereOperations)> GetEqCondition((string s1, string s2) p1, (string s3, string s4) p2, (string s5, string s6) p3)
        {
            return GetEqCondition(p1.s1, p1.s2)
                .Concat(GetEqCondition(p2.s3, p2.s4))
                .Concat(GetEqCondition(p3.s5, p3.s6))
                .ToList();
        }
        public static List<(string, string, SQLwhereOperations)> GetEqCondition((string s1, string s2) p1, (string s3, string s4) p2,
            (string s5, string s6) p3, (string s7, string s8) p4)
        {
            return GetEqCondition(p1.s1, p1.s2)
                .Concat(GetEqCondition(p2.s3, p2.s4))
                .Concat(GetEqCondition(p3.s5, p3.s6))
                .Concat(GetEqCondition(p4.s7, p4.s8))
                .ToList();
        }
    }

    public interface IRepository
    {
        List<T> GetAll<T>(string instanceName) where T : new();
        T GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new();
        IQueryable<T> GetByWhere<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new();
        void Insert<T>(string instanceName, T instance) where T : class;
        void Remove<T>(string instanceName, T instance) where T : class;
        void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class;
    }

    public class TASQLRepository : IRepository
    {
        static TASQLcontext Database { get; set; }
        static TASQLRepository()
        {
            if (Database == null)
            {
                //TASQLcontext is set by default
                Database = new TASQLcontext();
            }
        }
        public List<T>? GetAll<T>(string instanceName) where T : new() 
        {
            return (Database[instanceName] as IQueryable<T>)?.ToList();
        } 
        public T GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            var db = Database[instanceName] as DbSet<T>;
            IQueryable<T> filteredDb = db.AsNoTracking()?.AsQueryable();
            if (!isOr)
            {
                foreach (var (propSql, valSql, sqlOp) in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, propSql, valSql, sqlOp);
                }
            }
            if (isOr)
            {
                return whereConditions.SelectMany(w => GetWhereResult(filteredDb, w.propSql, w.valSql, w.sqlOp)).Distinct().FirstOrDefault();
            }
            return filteredDb.AsNoTracking()?.FirstOrDefault();
        }
        public IQueryable<T> GetByWhere<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            var db = Database[instanceName] as DbSet<T>;
            IQueryable<T> filteredDb = db.AsNoTracking()?.AsQueryable();
            if (!isOr)
            {
                foreach (var (propSql, valSql, sqlOp) in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, propSql, valSql, sqlOp);
                }
            }
            if (isOr)
            {
                return whereConditions.SelectMany(w => GetWhereResult(filteredDb, w.propSql, w.valSql, w.sqlOp)).Distinct().AsQueryable().AsNoTracking();
            }
            return filteredDb.AsNoTracking();
        }
        public void Insert<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>)?.Add(instance);
            Database.SaveChanges();
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            (Database[instanceName] as DbSet<T>)?.Remove(instance);
            Database.SaveChanges();
        }
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            var db = Database[instance] as DbSet<I>;
            IQueryable<I> filteredDb = db.AsNoTracking();
            {
                foreach (var (propSql, valSql, sqlOp) in whereConditions)
                {
                    filteredDb = GetWhereResult(filteredDb, propSql, valSql, sqlOp);
                }
                var flist = filteredDb.AsNoTracking()?.ToList();
            }

            try
            {
                foreach (var v in valuesToUpd)
                {
                    foreach (var f in filteredDb ?? Enumerable.Empty<I>())
                    {
                        var propertyInfo = f.GetType().GetProperty(v.Item1, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        propertyInfo?.SetValue(f, v.Item2, null);
                    }
                    Database.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                //_ = MessageBox.Show("Ошибка при обновлении ключевых столбцов.\nЕсли вы уверены, что хотите изменить эти данные" +
                //    " попробуйте использовать другой тип подключения.\nСообщение:\n" + ex.Message);
            }

            IQueryable<I> GetWhereResult(IQueryable<I> objQuerry, string tableColname, string tableValue, SQLwhereOperations oper)
            {
                return objQuerry.AsNoTracking()?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval() == tableValue).AsQueryable();
            }
        }
        private static IQueryable<T>? GetWhereResult<T>(IQueryable<T> objQuerry, string tableColname, string tableValue, SQLwhereOperations oper)
        {
            return oper switch
            {
                SQLwhereOperations.Less => objQuerry?.ToList().Where(q => double.Parse(q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval()) < double.Parse(tableValue)).AsQueryable(),
                SQLwhereOperations.More => objQuerry?.ToList().Where(q => double.Parse(q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval()) < double.Parse(tableValue)).AsQueryable(),
                SQLwhereOperations.StartsWith => objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q) != null && (bool)(q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval().ToLower().StartsWith(tableValue.ToLower()))).AsQueryable(),
                SQLwhereOperations.EndsWith => objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q) != null && (bool)(q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval().ToLower().EndsWith(tableValue.ToLower()))).AsQueryable(),
                SQLwhereOperations.Contains => objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q) != null && (bool)(q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval().ToLower().Contains(tableValue.ToLower()))).AsQueryable(),
                _ => objQuerry?.ToList().Where(q => q.GetType().GetProperty(tableColname)?.GetValue(q).ConvertSQLval() == tableValue).AsQueryable(),
            };
        }
        class TASQLcontext : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                string connectionString = "Server=localhost\\SQLEXPRESS;Database=Phone_Book;Trusted_Connection=True;";
                optionsBuilder.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
                {
                    _ = sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    );
                });
                optionsBuilder.EnableSensitiveDataLogging();
            }
            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<Departments>(entity => {
                    entity.ToTable("departments");
                });
                builder.Entity<Subdepartments>(entity => {
                    entity.ToTable("subdepartments");
                });
                builder.Entity<Person>(entity => {
                    entity.ToTable("person");
                });
                builder.Entity<Ops>(entity => {
                    entity.ToTable("ops");
                });
            }
            public virtual DbSet<Departments> departments { get; set; }
            public virtual DbSet<Subdepartments> subdepartments { get; set; }
            public virtual DbSet<Person> person { get; set; }
            public virtual DbSet<Ops> ops { get; set; }

            public object this[string propertyName]
            {
                get
                {
                    Type myType = typeof(TASQLcontext);
                    PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                    return myPropInfo?.GetValue(this, null);
                }
                set
                {
                    Type myType = typeof(TASQLcontext);
                    PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                    myPropInfo?.SetValue(this, value, null);
                }
            }
        }
    }

    public class ODBCRepository : IRepository
    {
        static OdbcConnection DbConnection { get; set; }
        static ODBCRepository()
        {
            OdbcConnectionStringBuilder builder = new OdbcConnectionStringBuilder() { Driver = "ODBC Driver 17 for SQL Server" };
            builder.Add("Server", "server_adress");
            builder.Add("Initial Catalog", "my_database");
            builder.Add("Uid", "Login");
            builder.Add("Pwd", "Password");
            DbConnection = new OdbcConnection(builder.ConnectionString);
            DbConnection.Open();
        }
        public List<T> GetAll<T>(string instanceName) where T : new()
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = $"SELECT * FROM {instanceName}";
            OdbcDataReader DbReader = DbCommand.ExecuteReader();
            {
                List<T> RetVal = new List<T>();
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        var Props = Entity.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        PropDict = Props.ToDictionary(p => p.Name.ToUpper(), p => p);
                        while (DbReader.Read())
                        {
                            T newObject = new T();
                            for (int Index = 0; Index < DbReader.FieldCount; Index++)
                            {
                                if (PropDict.ContainsKey(DbReader.GetName(Index).ToUpper()))
                                {
                                    var Info = PropDict[DbReader.GetName(Index).ToUpper()];
                                    if ((Info != null) && Info.CanWrite)
                                    {
                                        var Val = DbReader.GetValue(Index);
                                        Info.SetValue(newObject, (Val == DBNull.Value) ? null : Val, null);
                                    }
                                }
                            }
                            RetVal.Add(newObject);
                        }
                    }
                    else
                    {
                        RetVal = new List<T>();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                return RetVal;
            }
        }
        public T GetSingle<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = $"SELECT TOP 1 * FROM {instanceName} WHERE {GetWhereConditionStr(whereConditions, isOr)}";
            OdbcDataReader DbReader = DbCommand.ExecuteReader();
            {
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        Type type = typeof(T);
                        var accessor = TypeAccessor.Create(type);
                        var members = accessor.GetMembers();
                        var t = new T();

                        for (int i = 0; i < DbReader.FieldCount; i++)
                        {
                            if (!DbReader.IsDBNull(i))
                            {
                                string fieldName = DbReader.GetName(i);
                                if (members.Any(m => string.Equals(m.Name, fieldName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    accessor[t, fieldName] = DbReader.GetValue(i);
                                }
                            }
                        }
                        return t;
                    }
                    else
                    {
                        return new T();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public IQueryable<T> GetByWhere<T>(string instanceName, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false) where T : class, new()
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = $"SELECT * FROM {instanceName} WHERE {GetWhereConditionStr(whereConditions, isOr)}";
            OdbcDataReader DbReader = DbCommand.ExecuteReader();
            {
                List<T> RetVal = new List<T>();
                var Entity = typeof(T);
                var PropDict = new Dictionary<string, PropertyInfo>();
                try
                {
                    if (DbReader != null && DbReader.HasRows)
                    {
                        var Props = Entity.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        PropDict = Props.ToDictionary(p => p.Name.ToUpper(), p => p);
                        while (DbReader.Read())
                        {
                            T newObject = new T();
                            for (int Index = 0; Index < DbReader.FieldCount; Index++)
                            {
                                if (PropDict.ContainsKey(DbReader.GetName(Index).ToUpper()))
                                {
                                    var Info = PropDict[DbReader.GetName(Index).ToUpper()];
                                    if ((Info != null) && Info.CanWrite)
                                    {
                                        var Val = DbReader.GetValue(Index);
                                        Info.SetValue(newObject, (Val == DBNull.Value) ? null : Val, null);
                                    }
                                }
                            }
                            RetVal.Add(newObject);
                        }
                    }
                    else
                    {
                        RetVal = new List<T>();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                return RetVal.AsQueryable();
            }
        }
        public void Insert<T>(string instanceName, T instance) where T : class
        {
            CommonExecute($"INSERT INTO {instanceName} ({string.Join(", ", GetFields(instance))}) VALUES ({string.Join(", ", GetFieldValues(instance))})");
        }
        public void Remove<T>(string instanceName, T instance) where T : class
        {
            CommonExecute($"DELETE FROM {instanceName} WHERE {Converter(GetFields(instance), GetFieldValues(instance))}");
        }
        private List<string> GetFieldValues<T>(T instance) where T : class
        {
            var fieldValues = GetType(instance.GetType(), instance);
            if (!fieldValues.Any())
            {
                fieldValues = GetType(instance.GetType().BaseType, instance);
            }
            return fieldValues;
        }
        private void CommonExecute(string command)
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();
        }
        public void Update<T, I>(string instance, List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, List<(string, T)> valuesToUpd) where I : class
        {
            OdbcCommand DbCommand = DbConnection.CreateCommand();
            string command = $"UPDATE {instance} SET {ConcatValuesForUpdate(valuesToUpd)} WHERE {GetWhereConditionStr(whereConditions)}";
            DbCommand.CommandText = command;
            DbCommand.ExecuteNonQuery();
        }
        private static List<string> GetFields<T>(T _)
        {
            return typeof(T)
                .GetProperties()
                .Select(x =>
                {
                    var dbAttribute = x.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
                    return (dbAttribute == null || dbAttribute.Name == null) ? x.Name : dbAttribute.Name;
                })
                .Where(x => x != null)
                .ToList();
        }
        private static string ConcatValuesForUpdate<T>(List<(string, T)> valuesToUpd)
        {
            List<string> outputList = new List<string>();
            foreach (var v in valuesToUpd)
            {
                var convertedVal = v.Item2.ConvertSQLval() == "NULL" ? v.Item2.ConvertSQLval() : "'" + v.Item2.ConvertSQLval() + "'";
                if (convertedVal == "''") convertedVal = "NULL";

                outputList.Add(v.Item1 + " = " + convertedVal);
            }
            return string.Join(", ", outputList);
        }
        private static string GetWhereConditionStr(List<(string propSql, string valSql, SQLwhereOperations sqlOp)> whereConditions, bool isOr = false)
        {
            List<string> outputList = new();
            foreach (var (propSql, valSql, sqlOp) in whereConditions)
            {
                if (sqlOp is SQLwhereOperations.StartsWith)
                {
                    outputList.Add(propSql + " " + EnumOperToStr(sqlOp).Replace("%", valSql + '%'));
                }
                else if (sqlOp is SQLwhereOperations.EndsWith)
                {
                    outputList.Add(propSql + " " + EnumOperToStr(sqlOp).Replace("%", '%' + valSql));
                }
                else if (sqlOp is SQLwhereOperations.Contains)
                {
                    outputList.Add(propSql + " " + EnumOperToStr(sqlOp).Replace("%%", '%' + valSql + '%'));
                }
                else
                {
                    outputList.Add(propSql + EnumOperToStr(sqlOp) + "'" + valSql + "'");
                }
            }
            if (isOr) return string.Join(" or ", outputList);
            return string.Join(" and ", outputList);
        }
        private static string EnumOperToStr(SQLwhereOperations sqlOp)
        {
            switch (sqlOp)
            {
                case SQLwhereOperations.Equals: { return "="; }
                case SQLwhereOperations.Less: { return "<"; }
                case SQLwhereOperations.More: { return ">"; }
                case SQLwhereOperations.EndsWith: { return "LIKE '%'"; }
                case SQLwhereOperations.Contains: { return "LIKE '%%'"; }
                default: return "";
            }
        }
        private static List<string>? GetType<T>(Type type, T instance) where T : class
        {
            return type?
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field =>
                {
                    return field.GetValue(instance) == null ? "NULL" : "'" +
                    field.GetValue(instance).ConvertSQLval() + "'";
                })
                .ToList();
        }
        private static string Converter(List<string> fields, List<string> values)
        {
            string output = string.Empty;
            for (int i = 0; i < fields.Count; i++)
            {
                if (values[i] == "NULL") continue;
                if (i > 0) output += " AND ";
                output += fields[i] + "=" + values[i];
            }
            return output;
        }
    }

    public static class Validator
    {
        public static string? ConvertSQLval<T>(this T value)
        {
            bool isDecimal = decimal.TryParse(value?.ToString(), out decimal dec);
            bool isBool = bool.TryParse(value?.ToString(), out bool bo);
            return isDecimal ? dec.ToString().Replace(',', '.') : (isBool ? bo ? "1" : "0" : value?.ToString());
        }
    }

    public enum SQLwhereOperations
    {
        Equals,
        More,
        Less,
        StartsWith,
        EndsWith,
        Contains
    }
}
