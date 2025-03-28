// <copyright file="TrendChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Represents a decoder for parsing METAR trend information chunks such as "NOSIG", "BECMG", or "TEMPO".
/// </summary>
/// <remarks>
/// This class is responsible for recognizing and extracting trend-related data from METAR reports.
/// It extends the <see cref="MetarChunkDecoder"/> abstract class to provide specific implementation for decoding trend chunks.
/// </remarks>
public sealed class TrendChunkDecoder : MetarChunkDecoder
{
    // Surface Wind
    private const string WindDirectionRegexPattern = "(?:[0-9]{3}|VRB|///)";
    private const string WindSpeedRegexPattern = "P?(?:[/0-9]{2,3}|//)";
    private const string WindSpeedVariationsRegexPattern = "(?:GP?(?:[0-9]{2,3}))?";
    private const string WindUnitRegexPattern = "(?:KT|MPS|KPH)";

    // Prevailing Visibility
    private const string VisibilityRegexPattern = "(?:[0-9]{4})?";

    // Present Weather
    private const string CaracRegexPattern = "TS|FZ|SH|BL|DR|MI|BC|PR";
    private const string TypeRegexPattern = "DZ|RA|SN|SG|PL|DS|GR|GS|UP|IC|FG|BR|SA|DU|HZ|FU|VA|PY|DU|PO|SQ|FC|DS|SS|NSW|//";

    // Clouds
    private const string NoCloudRegexPattern = "(?:NSC|NCD|CLR|SKC)";
    private const string LayerRegexPattern = "(?:VV|FEW|SCT|BKN|OVC|///)(?:[0-9]{3}|///)(?:CB|TCU|///)?";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        const string windRegex =
            $"{WindDirectionRegexPattern}{WindSpeedRegexPattern}{WindSpeedVariationsRegexPattern}{WindUnitRegexPattern}";
        const string visibilityRegex = $"{VisibilityRegexPattern}|CAVOK";
        const string presentWeatherRegex =
            $@"(?:[-+]|VC)?(?:{CaracRegexPattern})?(?:{TypeRegexPattern})?(?:{TypeRegexPattern})?(?:{TypeRegexPattern})?";
        const string cloudRegex =
            $@"(?:{NoCloudRegexPattern}|(?:{LayerRegexPattern})(?: {LayerRegexPattern})?(?: {LayerRegexPattern})?(?: {LayerRegexPattern})?)";

        const string timeRegex = @"\s*(?:AT(\d{4}))?\s*(?:FM(\d{4}))?\s*(?:TL(\d{4}))?";
        const string trendGroup =
            $@"(TEMPO|BECMG|NOSIG){timeRegex}\s*({windRegex})?\s*({visibilityRegex})?\s*({presentWeatherRegex})?\s*({cloudRegex})?";
        return $@"{trendGroup}\s*((?=\s*(?:TEMPO|BECMG|NOSIG|$))(?:\s*{trendGroup})?)";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            var firstTrend = ParseTrendForecast(found, 1);
            result.Add("TrendForecast", firstTrend);

            if (!string.IsNullOrEmpty(found[9].Value))
            {
                var futureTrend = ParseTrendForecast(found, 10);
                result.Add("TrendForecastFuture", futureTrend);
            }
        }

        return GetResults(newRemainingMetar, result);
    }

    private TrendForecast ParseTrendForecast(List<Group> found, int startIndex)
    {
        var trend = new TrendForecast
        {
            ChangeIndicator = found[startIndex].Value switch
            {
                "NOSIG" => TrendForecastType.NoSignificantChanges,
                "BECMG" => TrendForecastType.Becoming,
                "TEMPO" => TrendForecastType.Temporary,
                _ => throw new ArgumentException("Invalid ChangeIndicator"),
            },
        };

        if (!string.IsNullOrEmpty(found[startIndex + 1].Value))
        {
            trend.AtTime = found[startIndex + 1].Value;
        }

        if (!string.IsNullOrEmpty(found[startIndex + 2].Value))
        {
            trend.FromTime = found[startIndex + 2].Value;
        }

        if (!string.IsNullOrEmpty(found[startIndex + 3].Value))
        {
            trend.UntilTime = found[startIndex + 3].Value;
        }

        if (!string.IsNullOrEmpty(found[startIndex + 4].Value))
        {
            trend.SurfaceWind = found[startIndex + 4].Value + " ";
        }

        if (!string.IsNullOrEmpty(found[startIndex + 5].Value))
        {
            trend.PrevailingVisibility = found[startIndex + 5].Value + " ";
        }

        if (!string.IsNullOrEmpty(found[startIndex + 6].Value))
        {
            trend.WeatherCodes = found[startIndex + 6].Value + " ";
        }

        if (!string.IsNullOrEmpty(found[startIndex + 7].Value))
        {
            trend.Clouds = found[startIndex + 7].Value + " ";
        }

        return trend;
    }
}
