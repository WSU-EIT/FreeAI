namespace FreeNavbarComponent.Client;

/// <summary>
/// The hardcoded city registry behind the Live Data sample menu and the
/// WeatherSample page. Every entry points at free, official, US-government-hosted
/// APIs, all of which serve "Access-Control-Allow-Origin: *" so Blazor
/// WebAssembly calls them directly from C# with no key, no proxy, and no
/// JavaScript:
///
///   - Forecast:      https://api.weather.gov (National Weather Service)
///   - Tides + water: https://api.tidesandcurrents.noaa.gov (NOAA CO-OPS)
///   - Sun and moon:  https://aa.usno.navy.mil (US Naval Observatory)
///
/// Local time has no equivalent API: time.gov is a JavaScript app with no public
/// REST endpoint (NIST distributes official time over NTP, which a browser cannot
/// speak), so the time view converts the device clock with the IANA time zone
/// database that ships with .NET instead.
/// </summary>
public static class WeatherSampleData
{
    public class City
    {
        /// <summary>URL-safe key used in the page's query string.</summary>
        public string Key = String.Empty;

        public string Name = String.Empty;

        public string State = String.Empty;

        /// <summary>Menu grouping, matching the time zones time.gov displays.</summary>
        public string TimeZoneGroup = String.Empty;

        /// <summary>IANA time zone id, resolvable by TimeZoneInfo in WebAssembly.</summary>
        public string TimeZoneId = String.Empty;

        public double Latitude;

        public double Longitude;

        /// <summary>NOAA CO-OPS station id used for tide predictions.</summary>
        public string TideStation = String.Empty;

        /// <summary>
        /// True only when the tide station carries the water_temperature product;
        /// not every station has the sensor, so the menu only offers the leaf
        /// where the data actually exists.
        /// </summary>
        public bool HasWaterTemp;
    }

    /// <summary>
    /// The timezone groups in menu order (east to west), matching time.gov.
    /// </summary>
    public static List<string> TimeZoneGroups {
        get {
            return new List<string> { "Eastern Time", "Pacific Time", "Alaska Time", "Hawaii Time" };
        }
    }

    public static List<City> Cities {
        get {
            return new List<City> {
                new City { Key = "newyork", Name = "New York", State = "NY", TimeZoneGroup = "Eastern Time", TimeZoneId = "America/New_York",
                    Latitude = 40.7128, Longitude = -74.0060, TideStation = "8518750", HasWaterTemp = true },
                new City { Key = "miami", Name = "Miami", State = "FL", TimeZoneGroup = "Eastern Time", TimeZoneId = "America/New_York",
                    Latitude = 25.7617, Longitude = -80.1918, TideStation = "8723214", HasWaterTemp = true },
                new City { Key = "seattle", Name = "Seattle", State = "WA", TimeZoneGroup = "Pacific Time", TimeZoneId = "America/Los_Angeles",
                    Latitude = 47.6062, Longitude = -122.3321, TideStation = "9447130", HasWaterTemp = false },
                new City { Key = "sanfrancisco", Name = "San Francisco", State = "CA", TimeZoneGroup = "Pacific Time", TimeZoneId = "America/Los_Angeles",
                    Latitude = 37.7749, Longitude = -122.4194, TideStation = "9414290", HasWaterTemp = false },
                new City { Key = "anchorage", Name = "Anchorage", State = "AK", TimeZoneGroup = "Alaska Time", TimeZoneId = "America/Anchorage",
                    Latitude = 61.2181, Longitude = -149.9003, TideStation = "9455920", HasWaterTemp = false },
                new City { Key = "honolulu", Name = "Honolulu", State = "HI", TimeZoneGroup = "Hawaii Time", TimeZoneId = "Pacific/Honolulu",
                    Latitude = 21.3069, Longitude = -157.8583, TideStation = "1612340", HasWaterTemp = true },
            };
        }
    }

    /// <summary>
    /// The data views offered for a city, as (query-string key, menu title)
    /// pairs. Water temperature only appears when the station supports it.
    /// </summary>
    public static List<(string Key, string Title)> ViewsFor(City city)
    {
        var output = new List<(string Key, string Title)> {
            ("forecast", "Forecast"),
            ("tides", "Tide Predictions"),
        };

        if (city.HasWaterTemp) {
            output.Add(("watertemp", "Water Temperature"));
        }

        output.Add(("sun", "Sun and Moon"));
        output.Add(("time", "Local Time"));

        return output;
    }

    public static City? FindCity(string? key)
    {
        return String.IsNullOrWhiteSpace(key)
            ? null
            : Cities.FirstOrDefault(x => x.Key == key.ToLower());
    }

    /// <summary>
    /// Builds the Live Data menu branch: timezone group, then city, then the
    /// data views the city's stations actually offer. Used from
    /// Helpers.MenuItemsApp so the menu and the page stay driven by one list.
    /// </summary>
    public static List<DataObjects.MenuItem> BuildMenuItems()
    {
        var output = new List<DataObjects.MenuItem>();

        int groupSort = 0;
        foreach (var group in TimeZoneGroups) {
            groupSort++;

            var groupItem = new DataObjects.MenuItem {
                Title = group,
                SortOrder = groupSort,
                Children = new List<DataObjects.MenuItem>(),
            };

            int citySort = 0;
            foreach (var city in Cities.Where(x => x.TimeZoneGroup == group)) {
                citySort++;

                var cityItem = new DataObjects.MenuItem {
                    Title = city.Name + ", " + city.State,
                    SortOrder = citySort,
                    Children = new List<DataObjects.MenuItem>(),
                };

                int viewSort = 0;
                foreach (var view in ViewsFor(city)) {
                    viewSort++;

                    cityItem.Children.Add(new DataObjects.MenuItem {
                        Title = view.Title,
                        SortOrder = viewSort,
                        url = Helpers.BuildUrl("WeatherSample") + "?city=" + city.Key + "&view=" + view.Key,
                    });
                }

                groupItem.Children.Add(cityItem);
            }

            output.Add(groupItem);
        }

        return output;
    }
}
