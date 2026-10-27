using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

public static class DumpMyAssets
{
    const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static async Task<string> Run()
    {
        var asm = typeof(EditorWindow).Assembly;
        var windowType = asm.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
        if (windowType == null) return "PackageManagerWindow type not found.";
        var windows = (IList)windowType.GetField("s_EnabledInstances", BF).GetValue(null);
        if (windows == null || windows.Count == 0) return "Package Manager window is not open.";
        var window = windows[0];
        var root = windowType.GetField("m_Root", BF).GetValue(window);
        var pageManager = root.GetType().GetField("m_PageManager", BF).GetValue(root);
        var pages = (IDictionary)pageManager.GetType().GetField("m_Pages", BF).GetValue(pageManager);
        var page = pages["MyAssets"];
        if (page == null) return "MyAssets page not found.";
        var visualStates = page.GetType().GetField("m_VisualStateList", BF).GetValue(page);

        var loadMore = page.GetType().GetMethod("LoadMore", BF);
        var started = DateTime.UtcNow;
        var loaded = GetProp<long>(visualStates, "countLoaded");
        var total = GetProp<long>(visualStates, "countTotal");
        while (loaded < total && (DateTime.UtcNow - started).TotalMinutes < 8)
        {
            var before = loaded;
            loadMore.Invoke(page, new object[] { 25L });
            var waits = 0;
            while (loaded == before && waits < 160 && (DateTime.UtcNow - started).TotalMinutes < 8)
            {
                await Task.Delay(250);
                loaded = GetProp<long>(visualStates, "countLoaded");
                waits++;
            }
            if (loaded == before) break;
        }

        var client = page.GetType().GetField("m_AssetStoreClient", BF).GetValue(page);
        try { client.GetType().GetMethod("FullScanLocalInfos", BF).Invoke(client, null); } catch { }

        var cache = client.GetType().GetField("m_AssetStoreCache", BF).GetValue(client);
        var purchases = (IDictionary)cache.GetType().GetField("m_PurchaseInfos", BF).GetValue(cache);
        var products = (IDictionary)cache.GetType().GetField("m_ProductInfos", BF).GetValue(cache);
        var locals = (IDictionary)cache.GetType().GetField("m_LocalInfos", BF).GetValue(cache);
        var updates = (IDictionary)cache.GetType().GetField("m_UpdateInfos", BF).GetValue(cache);

        var sb = new StringBuilder();
        sb.AppendLine("productId\tname\ttags\tpurchased\tcategory\tpublisher\tversion\tpublished\tsupportedUnity\tlocalPath\tlocalVersion\thasUpdate\thidden\tdescription");
        var count = 0;
        foreach (var purchase in purchases.Values.Cast<object>().OrderBy(p => GetField<string>(p, "displayName"), StringComparer.OrdinalIgnoreCase))
        {
            var id = GetField<long>(purchase, "productId");
            var product = products.Contains(id) ? products[id] : null;
            var local = locals.Contains(id) ? locals[id] : null;
            var update = updates.Contains(id) ? updates[id] : null;
            var tags = GetField<string[]>(purchase, "tags");
            var supported = product == null ? null : GetField<string[]>(product, "supportedVersions");
            var description = product == null ? "" : GetField<string>(product, "description");
            sb.Append(Escape(id.ToString())).Append('\t')
              .Append(Escape(GetField<string>(purchase, "displayName"))).Append('\t')
              .Append(Escape(tags == null ? "" : string.Join(",", tags))).Append('\t')
              .Append(Escape(GetField<string>(purchase, "purchasedTime"))).Append('\t')
              .Append(Escape(product == null ? "" : GetField<string>(product, "category"))).Append('\t')
              .Append(Escape(product == null ? "" : GetField<string>(product, "publisherName"))).Append('\t')
              .Append(Escape(product == null ? "" : GetField<string>(product, "versionString"))).Append('\t')
              .Append(Escape(product == null ? "" : GetField<string>(product, "publishedDate"))).Append('\t')
              .Append(Escape(supported == null ? "" : string.Join(";", supported))).Append('\t')
              .Append(Escape(local == null ? "" : GetField<string>(local, "packagePath"))).Append('\t')
              .Append(Escape(local == null ? "" : GetField<string>(local, "versionString"))).Append('\t')
              .Append(Escape(update == null ? "no" : "yes")).Append('\t')
              .Append(Escape(GetField<bool>(purchase, "isHidden") ? "yes" : "no")).Append('\t')
              .Append(Escape(description))
              .AppendLine();
            count++;
        }

        File.WriteAllText("/tmp/myassets_raw.tsv", sb.ToString(), new UTF8Encoding(false));
        return $"total={total} loaded={loaded} purchases={purchases.Count} products={products.Count} locals={locals.Count} updates={updates.Count} rows={count} file=/tmp/myassets_raw.tsv";
    }

    static T GetField<T>(object obj, string name)
    {
        var field = obj.GetType().GetField(name, BF);
        if (field == null) return default(T);
        var value = field.GetValue(obj);
        return value == null ? default(T) : (T)value;
    }

    static T GetProp<T>(object obj, string name)
    {
        var property = obj.GetType().GetProperty(name, BF);
        if (property == null) return default(T);
        var value = property.GetValue(obj);
        return value == null ? default(T) : (T)value;
    }

    static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
    }
}
