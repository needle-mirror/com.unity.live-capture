using System;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// 3D Axis flags. Useful to convert PositionAxis and RotationAxis.
    /// </summary>
    [Flags]
    public enum Axis
    {
        /// <summary>
        /// No Axis flags set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The X Axis flag.
        /// </summary>
        X = (1 << 0),
        /// <summary>
        /// The Y Axis flag.
        /// </summary>
        Y = (1 << 1),
        /// <summary>
        /// The Z Axis flag.
        /// </summary>
        Z = (1 << 2),
    }

    /// <summary>
    /// Position axis flags. Represents a camera rig position.
    /// </summary>
    [Flags]
    public enum PositionAxis
    {
        /// <summary>
        /// No Axis.
        /// </summary>
        [Description("No Axis.")]
        None = Axis.None,
        /// <summary>
        /// The Truck Axis, equivalent to the X Axis.
        /// </summary>
        [Description("The Truck Axis, equivalent to the X Axis.")]
        Truck = Axis.X,
        /// <summary>
        /// The Pedestal Axis, equivalent to the Y Axis.
        /// </summary>
        [Description("The Pedestal Axis, equivalent to the Y Axis.")]
        Pedestal = Axis.Y,
        /// <summary>
        /// The Dolly Axis, equivalent to the Z Axis.
        /// </summary>
        [Description("The Dolly Axis, equivalent to the Z Axis.")]
        Dolly = Axis.Z,
    }

    /// <summary>
    /// Rotation axis flags. Represents a camera rig rotation.
    /// </summary>
    [Flags]
    public enum RotationAxis
    {
        /// <summary>
        /// No Axis.
        /// </summary>
        [Description("No Axis.")]
        None = Axis.None,
        /// <summary>
        /// The Tilt Axis, equivalent to the X Axis.
        /// </summary>
        [Description("The Tilt Axis, equivalent to the X Axis.")]
        Tilt = Axis.X,
        /// <summary>
        /// The Pan Axis, equivalent to the Y Axis.
        /// </summary>
        [Description("The Pan Axis, equivalent to the Y Axis.")]
        Pan = Axis.Y,
        /// <summary>
        /// The Dutch Axis, equivalent to the Z Axis.
        /// </summary>
        [Description("The Dutch Axis, equivalent to the Z Axis.")]
        Dutch = Axis.Z,
    }
}
