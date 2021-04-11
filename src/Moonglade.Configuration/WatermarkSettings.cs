﻿namespace Moonglade.Configuration
{
    public class WatermarkSettings : IBlogSettings
    {
        public bool IsEnabled { get; set; }
        public bool KeepOriginImage { get; set; }
        public int FontSize { get; set; }
        public string WatermarkText { get; set; }
    }
}