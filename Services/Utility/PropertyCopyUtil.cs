using System.Reflection;

namespace TemperatureCharacteristics.Services.Utility
{
    /// ViewModel と PresetItem の階層構造を再帰的にコピーする共通ユーティリティ
    public static class PropertyCopyUtil
    {
        //*************************************************
        //source → target のプロパティを再帰的にコピーする
        //*************************************************
        public static void CopyPropertiesRecursive(object source, object target, IEnumerable<string>? ignoreList = null)
        {
            if (source == null || target == null)
                return;

            var ignore = ignoreList != null
                ? new HashSet<string>(ignoreList, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            var srcType = source.GetType();
            var dstType = target.GetType();

            var srcProps = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                    continue;

                if (ignore.Contains(srcProp.Name))
                    continue;

                var dstProp = dstType.GetProperty(srcProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (dstProp == null || !dstProp.CanWrite)
                    continue;

                var srcValue = srcProp.GetValue(source);

                // null はそのままコピー
                if (srcValue == null)
                {
                    dstProp.SetValue(target, null);
                    continue;
                }

                var srcPropType = srcProp.PropertyType;
                var dstPropType = dstProp.PropertyType;

                // プリミティブ / enum / string / decimal / nullable はそのままコピー
                if (IsSimpleType(srcPropType) && IsSimpleType(dstPropType))
                {
                    dstProp.SetValue(target, srcValue);
                    continue;
                }

                // IEnumerable（string 以外）は今回はスキップ（必要なら拡張）
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(srcPropType)
                    && srcPropType != typeof(string))
                {
                    continue;
                }

                // ネストオブジェクト → 再帰コピー
                var dstCurrentValue = dstProp.GetValue(target);
                if (dstCurrentValue == null)
                {
                    try
                    {
                        dstCurrentValue = Activator.CreateInstance(dstPropType);
                        dstProp.SetValue(target, dstCurrentValue);
                    }
                    catch
                    {
                        continue;
                    }
                }

                CopyPropertiesRecursive(srcValue, dstCurrentValue!, ignore);
            }
        }
        //*************************************************
        //プリミティブ型・enum・string・decimal・nullable を判定
        //*************************************************
        private static bool IsSimpleType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;

            return t.IsPrimitive
                   || t.IsEnum
                   || t == typeof(string)
                   || t == typeof(decimal)
                   || t == typeof(DateTime)
                   || t == typeof(TimeSpan);
        }
    }
}
