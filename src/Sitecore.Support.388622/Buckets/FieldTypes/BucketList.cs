using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.ContentSearch.Utilities;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.Buckets.Extensions;

namespace Sitecore.Support.Buckets.FieldTypes
{
  public class BucketList : Sitecore.Buckets.FieldTypes.BucketList
  {
    private void SearchContentLanguageByDefault(System.Collections.Specialized.NameValueCollection values)
    {
      Sitecore.Globalization.Language language;
      if (Sitecore.Globalization.Language.TryParse(this.ItemLanguage, out language))
      {
        var contextItem = Sitecore.Context.ContentDatabase.GetItem(base.ItemID, language);
        if (values["Language"].IsNullOrEmpty())
        {
          values["Language"] = contextItem.Language.CultureInfo.DisplayName.Replace("-", string.Empty)
              .Replace(" ", "_")
              .Replace("(", string.Empty)
              .Replace(")", string.Empty)
              .ToLowerInvariant();
        }
      }
    }

    protected override Item[] GetItems(Item current)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(current, "current");
      System.Collections.Specialized.NameValueCollection nameValues = Sitecore.StringUtil.GetNameValues(base.Source, '=', '&');
      string[] allKeys = nameValues.AllKeys;
      for (int j = 0; j < allKeys.Length; j++)
      {
        string name = allKeys[j];
        nameValues[name] = HttpUtility.JavaScriptStringEncode(nameValues[name]);
      }

      bool enableSetNewStartLocation = false;
      bool.TryParse(nameValues["EnableSetNewStartLocation"], out enableSetNewStartLocation);
      this.EnableSetNewStartLocation = enableSetNewStartLocation;

      string text = nameValues["StartSearchLocation"];
      if (Sitecore.Buckets.Extensions.StringExtensions.IsNullOrEmpty(text))
      {
        text = Sitecore.ItemIDs.RootID.ToString();
      }
      text = this.MakeFilterQueryable(text);
      if (!Sitecore.Buckets.Extensions.StringExtensions.IsGuid(text))
      {
        string paramValue = text;
        text = Sitecore.ItemIDs.RootID.ToString();
        this.LogSourceQueryError(current, "StartSearchLocation", paramValue, text);
      }
      string text2 = nameValues["Filter"];
      if (text2 != null)
      {
        System.Collections.Specialized.NameValueCollection nameValues2 = Sitecore.StringUtil.GetNameValues(text2, ':', '|');
        if (nameValues2.Count == 0 && text2 != string.Empty)
        {
          this.Filter = this.Filter + "&_content=" + text2;
        }
        foreach (string text3 in nameValues2.Keys)
        {
          this.Filter = string.Concat(new string[]
          {
            this.Filter,
            "&",
            text3,
            "=",
            nameValues2[text3]
          });
        }
      }
      List<SearchStringModel> list = Sitecore.Buckets.Extensions.StringExtensions.IsNullOrEmpty(text2) ? new List<SearchStringModel>() : SearchStringModel.ParseQueryString(text2).ToList<SearchStringModel>();
      list.Add(new SearchStringModel("_path", Sitecore.Buckets.Util.IdHelper.NormalizeGuid(text).ToLowerInvariant(), "must"));
      this.ExtractQueryStringAndPopulateModel(nameValues, list, "FullTextQuery", "_content", "_content", false);
      this.SearchContentLanguageByDefault(nameValues);
      this.ExtractQueryStringAndPopulateModel(nameValues, list, "Language", "language", "parsedlanguage", false);
      this.ExtractQueryStringAndPopulateModel(nameValues, list, "SortField", "sort", "sort", false);
      this.ExtractQueryStringAndPopulateModel(nameValues, list, "TemplateFilter", "template", "template", true);
      string text4 = nameValues["PageSize"];
      text4 = (Sitecore.Buckets.Extensions.StringExtensions.IsNullOrEmpty(text4) ? "10" : text4);
      int num;
      if (!int.TryParse(text4, out num))
      {
        num = 10;
        this.LogSourceQueryError(current, "PageSize", text4, num.ToString());
      }
      int num2 = Sitecore.Buckets.Extensions.StringExtensions.IsNullOrEmpty(text4) ? 10 : num;
      this.Filter = string.Concat(new object[]
      {
        this.Filter,
        "&location=",
        Sitecore.Buckets.Util.IdHelper.NormalizeGuid(Sitecore.Buckets.Extensions.StringExtensions.IsNullOrEmpty(text) ? Sitecore.Context.ContentDatabase.GetItem(base.ItemID).GetParentBucketItemOrRootOrSelf().ID.ToString() : text, true),
        "&pageSize=",
        num2
      });
      Item[] result;
      using (IProviderSearchContext providerSearchContext = ContentSearchManager.GetIndex((SitecoreIndexableItem)Sitecore.Context.ContentDatabase.GetItem(text)).CreateSearchContext(SearchSecurityOptions.Default))
      {
        IQueryable<Sitecore.ContentSearch.SearchTypes.SitecoreUISearchResultItem> source = LinqHelper.CreateQuery<Sitecore.ContentSearch.SearchTypes.SitecoreUISearchResultItem>(providerSearchContext, list);
        int num3 = source.Count<Sitecore.ContentSearch.SearchTypes.SitecoreUISearchResultItem>();
        this.PageNumber = ((num3 % num2 == 0) ? (num3 / num2) : (num3 / num2 + 1));
        List<Sitecore.ContentSearch.SearchTypes.SitecoreUISearchResultItem> source2 = source.Page(0, num2).ToList<Sitecore.ContentSearch.SearchTypes.SitecoreUISearchResultItem>();
        result = (from sitecoreItem in source2
              select sitecoreItem.GetItem() into i
              where i != null
              select i).ToArray<Item>();
      }
      return result;
    }
  }
}