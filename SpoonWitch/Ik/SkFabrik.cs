using ErisMath;

namespace SpoonWitch.Ik;

public static class SwFabrik
{
    private static readonly List<ErVec2> TempJoints = [];
    private static readonly List<double> Lengths = [];
    public static void Step(in List<ErVec2> joints, ErVec2 target, ErVec2 root)
    {
        if(joints.Count < 2) return;
        // Lengths.Clear();
        // for (int idx = 0; idx < joints.Count - 1; idx++)
        // {
        //     Lengths.Add((joints[idx+1]-joints[idx]).GetLength());
        // }
        Pass(joints,TempJoints,target);
        // for (int idx = 0; idx < Lengths.Count; idx++)
        // {
        //     Lengths[idx] = -Lengths[idx];
        // }
        // Lengths.Reverse();
        Pass(TempJoints,joints,root);
    }
    private static void Pass(List<ErVec2> joints, List<ErVec2> reversed, ErVec2 target)
    {
        reversed.Clear();
        Lengths.Clear();
        ErVec2 lastJoint = joints[0];
        for (int idx = 1; idx < joints.Count; idx++)
        {
            var offset = joints[idx] - lastJoint;
            Lengths.Add(offset.GetLength());
            lastJoint = joints[idx];
        }
        for (int idx = joints.Count - 2; idx > -1; idx--)
        {
            reversed.Add(target);
            ErVec2 basePos = joints[idx];
            ErVec2 dir = (basePos - target).Normalized();
            double len = Lengths[idx];
            target += dir * len;
        }
        reversed.Add(target);
    }
}
