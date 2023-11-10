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
