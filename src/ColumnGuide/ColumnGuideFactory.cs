﻿// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.ApplicationInsights.DataContracts;

namespace ColumnGuide
{
    #region Adornment Factory
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ColumnGuideAdornmentFactory : IWpfTextViewCreationListener, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered 
        /// below the text in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ColumnGuide")]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;

        /// <summary>
        /// Instantiates a ColumnGuide manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            // Always create the adornment, even if there are no guidelines, since we
            // respond to dynamic changes.
            new ColumnGuide(textView, TextEditorGuidesSettings, GuidelineBrush, Telemetry);
        }

        public void OnImportsSatisfied()
        {
            TrackSettings(global::ColumnGuide.Telemetry.CreateInitializeTelemetryItem(nameof(ColumnGuideAdornmentFactory) + " initialized"));

            GuidelineBrush.BrushChanged += (sender, newBrush) =>
            {
                Telemetry.Client.TrackEvent("GuidelineColorChanged", new Dictionary<string, string> { ["Color"] = newBrush.ToString() });
            };

            if (TextEditorGuidesSettings is INotifyPropertyChanged settingsChanged)
            {
                settingsChanged.PropertyChanged += OnSettingsChanged;
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITextEditorGuidesSettings.GuideLinePositionsInChars))
            {
                TrackSettings("SettingsChanged");
            }
        }

        private void TrackSettings(string eventName) => TrackSettings(new EventTelemetry(eventName));

        private void TrackSettings(EventTelemetry telemetry)
        {
            var telemetryProperties = telemetry.Properties;
            telemetryProperties.Add("Color", GuidelineBrush.Brush?.ToString() ?? "unknown");

            var count = 0;
            foreach (var column in TextEditorGuidesSettings.GuideLinePositionsInChars)
            {
                telemetryProperties.Add("guide" + count.ToString(CultureInfo.InvariantCulture), column.ToString(CultureInfo.InvariantCulture));
                count++;
            }

            telemetry.Metrics.Add("Count", count);

            Telemetry.Client.TrackEvent(telemetry);
        }

        [Import]
        private ITextEditorGuidesSettings TextEditorGuidesSettings { get; set; }

        [Import]
        private ITelemetry Telemetry { get; set; }

        [Import]
        private GuidelineBrush GuidelineBrush { get; set; }
    }
    #endregion //Adornment Factory
}
