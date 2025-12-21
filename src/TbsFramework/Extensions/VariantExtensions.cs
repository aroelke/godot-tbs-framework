using System;
using Godot;

namespace TbsTemplate.Extensions;

public static class VariantExtensions
{
    public static bool ValueEquals(this Variant left, Variant right) => left.VariantType == right.VariantType && left.VariantType switch
    {
        Variant.Type.Nil => true,
        Variant.Type.Bool => left.AsBool().Equals(right.AsBool()),
        Variant.Type.Int => left.AsInt64().Equals(right.AsInt64()),
        Variant.Type.Float => left.AsDouble().Equals(right.AsDouble()),
        Variant.Type.String => left.AsString().Equals(right.AsString()),
        Variant.Type.Vector2 => left.AsVector2().Equals(right.AsVector2()),
        Variant.Type.Vector2I => left.AsVector2I().Equals(right.AsVector2I()),
        Variant.Type.Rect2 => left.AsRect2().Equals(right.AsRect2()),
        Variant.Type.Rect2I => left.AsRect2I().Equals(right.AsRect2I()),
        Variant.Type.Vector3 => left.AsVector3().Equals(right.AsVector3()),
        Variant.Type.Vector3I => left.AsVector3I().Equals(right.AsVector3I()),
        Variant.Type.Transform2D => left.AsTransform2D().Equals(right.AsTransform2D()),
        Variant.Type.Vector4 => left.AsVector4().Equals(right.AsVector4()),
        Variant.Type.Vector4I => left.AsVector4I().Equals(right.AsVector4I()),
        Variant.Type.Plane => left.AsPlane().Equals(right.AsPlane()),
        Variant.Type.Quaternion => left.AsQuaternion().Equals(right.AsQuaternion()),
        Variant.Type.Aabb => left.AsAabb().Equals(right.AsAabb()),
        Variant.Type.Basis => left.AsBasis().Equals(right.AsBasis()),
        Variant.Type.Transform3D => left.AsTransform3D().Equals(right.AsTransform3D()),
        Variant.Type.Projection => left.AsProjection().Equals(right.AsProjection()),
        Variant.Type.Color => left.AsColor().Equals(right.AsColor()),
        Variant.Type.StringName => left.AsStringName().Equals(right.AsStringName()),
        Variant.Type.NodePath => left.AsNodePath().Equals(right.AsNodePath()),
        Variant.Type.Rid => left.AsRid().Equals(right.AsRid()),
        Variant.Type.Object => left.AsGodotObject().Equals(right.AsGodotObject()),
        Variant.Type.Callable => left.AsCallable().Equals(right),
        Variant.Type.Signal => left.AsSignal().Equals(right.AsSignal()),
        Variant.Type.Dictionary => left.AsGodotDictionary().Equals(right.AsGodotDictionary()),
        Variant.Type.Array => left.AsGodotArray().Equals(right.AsGodotArray()),
        Variant.Type.PackedByteArray => left.AsByteArray().Equals(right.AsByteArray()),
        Variant.Type.PackedInt32Array => left.AsInt32Array().Equals(right.AsInt32Array()),
        Variant.Type.PackedInt64Array => left.AsInt64Array().Equals(right.AsInt64Array()),
        Variant.Type.PackedFloat32Array => left.AsFloat32Array().Equals(right.AsFloat32Array()),
        Variant.Type.PackedFloat64Array => left.AsFloat64Array().Equals(right.AsFloat64Array()),
        Variant.Type.PackedStringArray => left.AsStringArray().Equals(right.AsStringArray()),
        Variant.Type.PackedVector2Array => left.AsVector2Array().Equals(right.AsVector2Array()),
        Variant.Type.PackedVector3Array => left.AsVector3Array().Equals(right.AsVector3Array()),
        Variant.Type.PackedColorArray => left.AsColorArray().Equals(right.AsColorArray()),
        _ => throw new ArgumentOutOfRangeException(left.VariantType.ToString())
    };
}