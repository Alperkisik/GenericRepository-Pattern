using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace GenericRepository_Pattern.Extensions
{
    public static class SqlExtensions
    {
        static T GetInstance<T>()
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        public static T ReadSingle<T>(this SqlDataReader reader)
        {
            if (!reader.Read()) return default;

            //if typeof(T) is a System Type such as string,int,datetime etc
            if (typeof(T).FullName.Contains("System")) return (T)Convert.ChangeType(reader[0], typeof(T));


            //if typeof(T) is a class or Entity
            T dataObject = GetInstance<T>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var property = dataObject.GetType().GetProperty(reader.GetName(i));
                if (property == null) continue;

                if (reader[i] != DBNull.Value) property.SetValue(dataObject, reader[i]);
            }

            return dataObject;
        }

        public static IEnumerable<T> ReadList<T>(this SqlDataReader reader)
        {
            var data = new List<T>();

            //if typeof(T) is a System Type such as string,int,datetime etc
            if (typeof(T).FullName.Contains("System"))
            {
                while (reader.Read())
                {
                    data.Add((T)Convert.ChangeType(reader[0], typeof(T)));
                }

                return data;
            }

            //if typeof(T) is a class or Entity
            while (reader.Read())
            {
                T dataObject = GetInstance<T>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var property = dataObject.GetType().GetProperty(reader.GetName(i));
                    if (property == null) continue;

                    if (reader[i] != DBNull.Value) property.SetValue(dataObject, reader[i]);
                }

                data.Add(dataObject);
            }

            return data;
        }
    }

}
