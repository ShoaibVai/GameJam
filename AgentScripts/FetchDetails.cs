using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

public static class FetchDetails
{
    const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    public static async Task<string> Run(int limit)
    {
        var asm = typeof(EditorWindow).Assembly;
        var windowType = asm.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
        var windows = (IList)windowType.GetField("s_EnabledInstances", BF).GetValue(null);
        var window = windows[0];
        var root = windowType.GetField("m_Root", BF).GetValue(window);
        var pageManager = root.GetType().GetField("m_PageManager", BF).GetValue(root);
        var pages = (IDictionary)pageManager.GetType().GetField("m_Pages", BF).GetValue(pageManager);
        var page = pages["MyAssets"];
        var client = page.GetType().GetField("m_AssetStoreClient", BF).GetValue(page);
        var cache = client.GetType().GetField("m_AssetStoreCache", BF).GetValue(client);
        var products = (IDictionary)cache.GetType().GetField("m_ProductInfos", BF).GetValue(cache);
        var fetch = client.GetType().GetMethod("FetchProductInfo", BF);

        var missing = new List<long>();
        foreach (var line in File.ReadAllLines("/tmp/myassets_raw.tsv").Skip(1))
        {
            var cols = line.Split('\t');
            if (cols.Length < 5 || !string.IsNullOrEmpty(cols[4])) continue;
            long id;
            if (long.TryParse(cols[0], out id) && !products.Contains(id)) missing.Add(id);
        }
        if (limit > 0 && missing.Count > limit) missing = missing.Take(limit).ToList();

        var sw = Stopwatch.StartNew();
        var sem = new SemaphoreSlim(6);
        var failed = new List<long>();
        var doneCount = 0;

        foreach (var chunk in Chunk(missing, 30))
        {
            var tasks = chunk.Select(async id =>
            {
                await sem.WaitAsync();
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var released = 0;
                Action release = () =>
                {
                    if (Interlocked.Exchange(ref released, 1) == 0) sem.Release();
                };
                Action onDone = () =>
                {
                    release();
                    tcs.TrySetResult(true);
                };
                try
                {
                    fetch.Invoke(client, new object[] { id, onDone });
                }
                catch
                {
                    release();
                    tcs.TrySetResult(false);
                }
                var finished = await Task.WhenAny(tcs.Task, Task.Delay(20000));
                release();
                if (finished != tcs.Task || !products.Contains(id))
                {
                    lock (failed) failed.Add(id);
                }
                else
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            await Task.WhenAll(tasks);
        }

        var sb = new StringBuilder();
        sb.AppendLine("productId\tcategory\tpublisher\tversion\tpublished\tfirstPublished\tsupportedUnity\tpackageName\tstoreUrl\tpublisherUrl\tpublisherWebsite\tpublisherSupport\tdescription");
        foreach (var product in products.Values.Cast<object>().OrderBy(p => GetField<long>(p, "productId")))
        {
            var supported = GetField<string[]>(product, "supportedVersions");
            sb.Append(GetField<long>(product, "productId")).Append('\t')
              .Append(Escape(GetField<string>(product, "category"))).Append('\t')
              .Append(Escape(GetField<string>(product, "publisherName"))).Append('\t')
              .Append(Escape(GetField<string>(product, "versionString"))).Append('\t')
              .Append(Escape(GetField<string>(product, "publishedDate"))).Append('\t')
              .Append(Escape(GetField<string>(product, "firstPublishedDate"))).Append('\t')
              .Append(Escape(supported == null ? "" : string.Join(";", supported))).Append('\t')
              .Append(Escape(GetField<string>(product, "packageName"))).Append('\t')
              .Append(Escape(GetField<string>(product, "assetStoreProductUrl"))).Append('\t')
              .Append(Escape(GetField<string>(product, "assetStorePublisherUrl"))).Append('\t')
              .Append(Escape(GetField<string>(product, "publisherWebsiteUrl"))).Append('\t')
              .Append(Escape(GetField<string>(product, "publisherSupportUrl"))).Append('\t')
              .Append(Escape(GetField<string>(product, "description")))
              .AppendLine();
        }
        File.WriteAllText("/tmp/myassets_products.tsv", sb.ToString(), new UTF8Encoding(false));

        var failText = string.Join(",", failed.Take(100));
        return $"requested={missing.Count} fetched={doneCount} failed={failed.Count} products={products.Count} elapsed={sw.Elapsed.TotalSeconds:F1}s failIds={failText}";
    }

    static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size) yield return source.GetRange(i, Math.Min(size, source.Count - i));
    }

    static T GetField<T>(object obj, string name)
    {
        var field = obj.GetType().GetField(name, BF);
        if (field == null) return default(T);
        var value = field.GetValue(obj);
        return value == null ? default(T) : (T)value;
    }

    static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
    }
}
