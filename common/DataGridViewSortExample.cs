using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

public static class ListToDataTableConverter
{
    public static DataTable ToDataTable<T>(this List<T> items)
    {
        var dataTable = new DataTable(typeof(T).Name);

        // 获取所有的属性
        PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo prop in Props)
        {
            // 设置列名和数据类型
            dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        }
        foreach (T item in items)
        {
            var values = new object[Props.Length];
            for (int i = 0; i < Props.Length; i++)
            {
                values[i] = Props[i].GetValue(item, null);
            }
            dataTable.Rows.Add(values);
        }
        return dataTable;
    }
}
