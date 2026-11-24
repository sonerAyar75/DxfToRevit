namespace DxfToRevit.Core.Openings;

/// <summary>How the host creates an opening.</summary>
public enum OpeningCreationMode
{
    /// <summary>A loadable family instance (door or window) hosted by the wall.</summary>
    HostedFamily,

    /// <summary>A rectangular cut in the wall without a family.</summary>
    WallOpening
}
