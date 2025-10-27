using CsvHelper.Configuration;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Terramon.DataGen.Models;

public class MoveCsvModel
{
    public ushort ID { get; set; }
    public required string Identifier { get; set; }
    public byte GenerationID { get; set; }
    public ushort TypeID { get; set; }
    public byte? Power { get; set; }
    public byte? PP { get; set; }
    public byte? Accuracy { get; set; }
    public sbyte Priority { get; set; }
    public byte TargetID { get; set; }
    public byte DamageClassID { get; set; }
    public ushort? EffectID { get; set; }
    public byte? EffectChance { get; set; }
    public byte? ContestTypeID { get; set; }
    public ushort? ContestEffectID { get; set; }
    public ushort? SuperContestEffectID { get; set; }
}

public sealed class MoveCsvMap : ClassMap<MoveCsvModel>
{
    public MoveCsvMap()
    {
        Map(m => m.ID).Name("id");
        Map(m => m.Identifier).Name("identifier");
        Map(m => m.GenerationID).Name("generation_id");
        Map(m => m.TypeID).Name("type_id");
        Map(m => m.Power).Name("power");
        Map(m => m.PP).Name("pp");
        Map(m => m.Accuracy).Name("accuracy");
        Map(m => m.Priority).Name("priority");
        Map(m => m.TargetID).Name("target_id");
        Map(m => m.DamageClassID).Name("damage_class_id");
        Map(m => m.EffectID).Name("effect_id");
        Map(m => m.EffectChance).Name("effect_chance");
        Map(m => m.ContestTypeID).Name("contest_type_id");
        Map(m => m.ContestEffectID).Name("contest_effect_id");
        Map(m => m.SuperContestEffectID).Name("super_contest_effect_id");
    }
}