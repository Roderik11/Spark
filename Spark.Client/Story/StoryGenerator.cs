using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Spark.Client
{
    public enum StoryFragmentType
    {
        Race
    }

    public enum StoryFragmentUsage
    {
        Adjective,
        Adverb,
        Noun
    }

    // should be a flag to allow attribute usage for more than one context
    // (e.g. "strange" for diet and personality, "adorable" for physique and occupations etc.)
    public enum StoryFragmentContext
    {
        Physique,
        Diet,
        Personality,
        Occupation,
        Belief,
        Relations,
        Politics,
        Manner,
        Place,
        Frequency
    }

    public class StoryGenerator
    {
        public long Seed { get; set; }
        public Random Random { get; set; }

        public static DataSet Data { get; set; }

        static StoryGenerator()
        {
            Data = new DataSet("StoryData");

            #region adjectives

            DataTable adjectives = new DataTable("Adjective");
            adjectives.Columns.Add("ID", typeof(Guid));
            adjectives.Columns.Add("Context", typeof(StoryFragmentContext));
            adjectives.Columns.Add("Quality", typeof(int));
            adjectives.Columns.Add("Value", typeof(string));
            adjectives.PrimaryKey = new DataColumn[] { adjectives.Columns[0] };
            Data.Tables.Add(adjectives);

            // -1 bad
            //  0 neutral
            //  1 good

            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "tall");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "short");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "fat");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "slender");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "muscular");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "scaly");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "delicate");

            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "bitter");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "sweet");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "salty");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "sour");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "tasteless");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "poisonous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "hallucinogenic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "wholesome");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "minced");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "boiled");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "raw");

            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "adorable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "agreeable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "alert");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "alluring");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "ambitious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "amused");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "brave");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "bright");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "calm");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "capable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "charming");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "cheerful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "confident");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "cooperative");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "courageous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "cultured");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "dashing");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "dazzling");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "decisive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "decorous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "delightful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "determined");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "diligent");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "discreet");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "dynamic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "eager");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "efficient");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "elated");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "enchanting");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "energetic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "entertaining");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "enthusiastic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "exciting");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "exuberant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "fabulous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "fair");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "faithful"); 
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "fantastic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "fearless");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "friendly");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "funny");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "generous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "gentle");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "glorious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "happy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "harmonious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "helpful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "hilarious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "honorable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "industrious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "instinctive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "jolly");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "joyous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "kind");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "kind-hearted");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "knowledgeable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "level - headed");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "likeable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "lively");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "lovely"); 
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "mature");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "modern");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "nice");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "obedient"); 
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "peaceful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "placid");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "pleasant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "plucky");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "productive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "protective");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "proud");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "quiet");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "resolute");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "righteous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "romantic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "self-assured");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "sensitive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "shrewd");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "sincere");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "skilful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "smiling");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "splendid");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "steadfast");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "talented");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "thoughtful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "thrifty");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "tough");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "trustworthy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "unusual");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "upbeat");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "vigorous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "vivacious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "warm");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "wise");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "witty");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 1, "wonderful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "zany");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "zealous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "abrasive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "abusive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "angry");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "annoying");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "anxious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "arrogant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "awful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "belligerent");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "boorish");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "boring");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "callous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "careless");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "clumsy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "combative"); 
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "cowardly");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "crazy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "creepy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "cruel");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "cynical");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "dangerous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "deceitful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "defective");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "defiant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "demonic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "depressed");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "deranged");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "disagreeable");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "disturbing");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "domineering");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "draconian");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "envious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "erratic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "evasive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "evil");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "fanatical");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "fierce");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "filthy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "finicky");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "flashy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "flippant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "foolish");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "frantic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "fretful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "frightened"); 
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "furtive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "greedy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "grouchy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "gruesome");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "grumpy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "gullible");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "hesitant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "horrible");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "ignorant");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "irresolute");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "jealous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "jittery");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "lazy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "lonely");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "malicious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "materialistic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "mean");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "mysterious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "naive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "nasty");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "naughty");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "nervous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "noisy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "obnoxious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "outrageous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "pathetic");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "possessive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "quarrelsome");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "repulsive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "ruthless");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "sad");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "scary");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "secretive");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "selfish");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "silly");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "slow");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "sneaky");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "snobbish");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "squeamish");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "stingy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "strange");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "sulky");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "tacky");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "tense");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "terrible");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "testy");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "thoughtless");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "timid");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "tiresome");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "troubled");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "upset");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "uptight");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "vengeful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "venomous");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "volatile");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "voracious");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "vulgar");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "wary");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "wasteful");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "weak");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, 0, "weary");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "wicked");
            adjectives.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Personality, -1, "wretched");

            //attributes.WriteXml("G:\\tests\\attribs.xml");

            #endregion

            #region adverbs

            DataTable adverbs = new DataTable("Adverb");
            adverbs.Columns.Add("ID", typeof(Guid));
            adverbs.Columns.Add("Context", typeof(StoryFragmentContext));
            adverbs.Columns.Add("Quality", typeof(int));
            adverbs.Columns.Add("Value", typeof(string));
            adverbs.PrimaryKey = new DataColumn[] { adverbs.Columns[0] };
            Data.Tables.Add(adverbs);

            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "carefully");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "eagerly");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "easily");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "loudly");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "patiently");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "quickly");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "quietly");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Manner, 0, "gingerly");

            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Place, 0, "abroad");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Place, 0, "at home");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Place, 0, "inside");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Place, 0, "outside");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Place, 0, "underground");

            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Frequency, 1, "often");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Frequency, -1, "rarely");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Frequency, -1, "seldom");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Frequency, 0, "sometimes");
            adverbs.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Frequency, 1, "usually");

            #endregion

            #region adverbs

            DataTable nouns = new DataTable("Noun");
            nouns.Columns.Add("ID", typeof(Guid));
            nouns.Columns.Add("Context", typeof(StoryFragmentContext));
            nouns.Columns.Add("Quality", typeof(int));
            nouns.Columns.Add("Value", typeof(string));
            nouns.PrimaryKey = new DataColumn[] { nouns.Columns[0] };
            Data.Tables.Add(nouns);

            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "bodies");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "arms");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "hands");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "fingers");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "legs");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "feet");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "heads");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "eyes");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "ears");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "noses");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Physique, 0, "horns");

            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "vegetables");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "grains");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "meat");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "algae");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "fungi");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "leaves");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "bark strips");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "insects");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Diet, 0, "herbs");

            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Occupation, 0, "read books");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Occupation, 0, "play ball");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Occupation, 0, "take a walk with their pets");
            nouns.Rows.Add(Guid.NewGuid(), StoryFragmentContext.Occupation, 0, "meet with friends");

            #endregion

            DataTable raceText = new DataTable("RaceText");
            raceText.Columns.Add("ID", typeof(Guid));
            raceText.Columns.Add("Value", typeof(string));
            Data.Tables.Add(raceText);

            raceText.Rows.Add(Guid.NewGuid(),
                "The <NAME> are a <Adjective:Physique>, <Adjective:Physique> race with <Adjective:Physique> <Noun:Physique>. " +
                "They consume <Adjective:Diet> <Noun:Diet>. " +
                "<Adjective:Personality:Quality> in nature, they are <Adverb:Frequency:QualityInverted> <Adjective:Personality:QualityInverted> when <Adverb:Place>. " +
                "Their mood changes to be a little more <Adjective:Personality:QualityInverted> only when they <Noun:Occupation>, which they practice <Adverb:Manner>, <Adverb:Frequency> <Adverb:Place>."
            );

            //raceText.Rows.Add(Guid.NewGuid(),
            //    "The <NAME> are a <0:0:0>, <0:0:1> race with <0:0:2>. " +
            //    "They eat <1:0:0> <1:0:1> <1:0:2>. " + 
            //    "<2:0:0> in nature, they are <2:0:1> <2:0:2>. " + 
            //    "Their favorite pastime is <3:0:0> <3:0:1> <3:0:2>. " + 
            //    "Believing that <4:0:0> <4:0:1>, they <4:0:2> to <4:0:3>. " + 
            //    "Their policy towards outsiders is simple: Being " + 
            //    "<5:0:0> <5:0:1>, they have been <5:0:2> <5:0:3> <5:0:4>. " +
            //    "For a few generations the <NAME> have been living in a society governed by <6:0:0> <6:0:1> with <6:0:2> <6:0:3>."
            //    );

            //raceText.Rows.Add(Guid.NewGuid(),
            //    "For the <0:0:0>, <0:0:1> <NAME> with their <0:0:2>, " +
            //    "<1:0:0> <1:0:1> <1:0:2> are a deliciacy. " +
            //    "Others describe them as <2:0:0> - <2:0:2> they can become very <2:0:1>. " +
            //    "When not otherwise occupied, <NAME> can be found immersed in <3:0:0> <3:0:1> <3:0:2>. " +
            //    "Their beliefs are rooted in the assumption that <4:0:0> <4:0:1>, so they <4:0:2> in order to <4:0:3>. " +
            //    "<5:0:4>, their <5:0:0> <5:0:1> have been <5:0:2> <5:0:3>. " +
            //    "The current <6:0:2> <6:0:3> is the result of a century-old <6:0:0> <6:0:1>."
            //    );

            //raceText.Rows.Add(Guid.NewGuid(),
            //    "<5:0:4>, <5:0:0> <NAME> <5:0:1> have been <5:0:2> <5:0:3>. " + 
            //    "The <1:0:0> <1:0:1> <1:0:2> that they depend upon for nutrition are in sparse supply. " +
            //    "<0:0:0> and <0:0:1> in stature and equipped with <0:0:2>, a <NAME> is easily discernable even in a crowd. " + 
            //    "<2:0:1> <2:0:2>, they can also become quite <2:0:0> with age. " + 
            //    "One of the most proficient races ever to attempt <3:0:0>, one can witness them practice <3:0:2> <3:0:1>. " + 
            //    "They will gladly entertain strangers with their beliefs, claiming that <4:0:0> <4:0:1>, and are always eager to <4:0:2> trying to <4:0:3>. " +
            //    "The <NAME> live in a <6:0:0> political structure that can best be described as a <6:0:1> and thus usually show a <6:0:2> <6:0:3>."
            //    );
        }

        public StoryGenerator()
        {
            Random = new Random();
        }

        public PlanetStory GeneratePlanetStory()
        {
            PlanetStory result = new PlanetStory();
            return result;
        }

        public RaceStory GenerateRaceStory()
        {
            RaceStory result = new RaceStory();
            result.Name = "Test Race";

            // get tag text
            result.Description = GetText("RaceText", "");

            // add race name
            result.Description = result.Description.Replace("<NAME>", result.Name);
            
            // get race alignment (bad, neutral, good) for picking purposes
            int alignment = Random.Next(-1, 2);
            int alignmentInverted = alignment * -1;

            result.Description = result.Description.Replace(":QualityInverted", ":" + alignmentInverted.ToString());
            result.Description = result.Description.Replace(":Quality", ":" + alignment.ToString());

            string tag = string.Empty;
            string[] tagData = null;
            string expression = string.Empty;

            int tagIndex = 0;
            int tagStart = result.Description.IndexOf("<", 0);
            int tagEnd = 0;

            while (tagStart > -1)
            {               
                tagEnd = result.Description.IndexOf(">", tagStart + 1);
                tag = result.Description.Substring(tagStart, tagEnd - tagStart + 1);
                tagData = tag.Replace("<", "").Replace(">", "").Split(':');

                if(tagData.Length == 2)
                    expression = string.Format("Context = {0}", (int)Enum.Parse(typeof(StoryFragmentContext), tagData[1]));
                else if(tagData.Length == 3)
                    expression = string.Format("Context = {0} AND Quality = {1}", (int)Enum.Parse(typeof(StoryFragmentContext), tagData[1]), tagData[2]);

                result.Description = result.Description.Remove(tagStart, tagEnd - tagStart + 1);
                result.Description = result.Description.Insert(tagStart, GetData(tagData[0], expression));

                tagStart = result.Description.IndexOf("<", tagStart);
                tagIndex = tagEnd + 1;
            }

            return result;
        }

        private string GetText(string table, string expression)
        {
            DataRow[] rows = Data.Tables[table].Select(expression);

            if (rows.Length <= 0) return string.Empty;

            return rows[Random.Next(0, rows.Length)][1].ToString();
        }

        private string GetData(string table, string expression)
        {
            DataRow[] rows = Data.Tables[table].Select(expression);

            if (rows.Length <= 0) return string.Empty;

            return rows[Random.Next(0, rows.Length)][3].ToString();
        }
    }
}


/*
            DataTable raceAttributes = new DataTable("RaceData");
            raceAttributes.Columns.Add("ID", typeof(Guid));
            raceAttributes.Columns.Add("Type", typeof(StoryFragmentType));
            raceAttributes.Columns.Add("Context", typeof(StoryFragmentContext));
            raceAttributes.Columns.Add("Usage", typeof(StoryFragmentUsage));
            raceAttributes.Columns.Add("Variation", typeof(int));
            raceAttributes.Columns.Add("Value", typeof(string));
            Data.Tables.Add(raceAttributes);

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 0, "tall");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 0, "short");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 0, "human-sized");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 1, "fat");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 1, "slender");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 1, "muscular");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 2, "unusually big eyes");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 2, "very delicate hands");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 2, "severely bent spines");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 2, "scaly skin");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Physique, StoryFragmentUsage.Adjective, 2, "pointy horns");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 0, "mostly algae");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 0, "scraps of tree bark");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 0, "spikey fish");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 0, "furtive mammals");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 0, "colorless mushrooms");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 1, "exhibiting a pungent smell");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 1, "unheard of in other parts of the sector");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 1, "tasting almost like vanilla");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 2, "served with a bitter sauce");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 2, "which they need large quantities of to survive");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Diet, StoryFragmentUsage.Adjective, 2, "and poisonous to all but themselves");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 0, "laid back and mellow");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 0, "aggressive");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 0, "indifferent");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 0, "cunning");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 0, "gentle");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 1, "restless");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 1, "serene");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 1, "furious");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 2, "when provoked");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 2, "during mating season");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Personality, StoryFragmentUsage.Adjective, 2, "at night");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 0, "fishing");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 0, "gambling");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 0, "board games");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 0, "expressive dancing");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 1, "all by themselves");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 1, "with friends");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 1, "in large groups");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 2, "in regular intervals");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 2, "whenever there is time");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Occupation, StoryFragmentUsage.Adjective, 2, "for weeks on end");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 0, "the reign of their ancient gods");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 0, "nothing but their own supremacy");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 0, "prophecies and ancient scriptures");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 0, "the arrival of an unknown messiah");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 1, "will rule them");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 1, "will prevail");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 1, "will come to pass");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 2, "gather in silence");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 2, "sacrifice infidels on altar stones");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 2, "plot and scheme");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 3, "find the truth");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 3, "see the future");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Belief, StoryFragmentUsage.Adjective, 3, "gather strength");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 0, "ruthless");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 0, "stern");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 0, "diplomatic");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 0, "unpredictable");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 1, "settlers");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 1, "warriors");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 1, "businessmen");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 1, "nomads");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 2, "embracing");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 2, "avoiding");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 2, "provoking");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 3, "anyone around them");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 3, "neighbors");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 3, "friends and foe alike");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 4, "since the beginning of time");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 4, "recently");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Relations, StoryFragmentUsage.Adjective, 4, "for years");

            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 0, "liberal");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 0, "totalitarian");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 0, "anarchic");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 0, "socialist");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 1, "monarchy");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 1, "oligarchy");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 1, "democracy");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 1, "caste system");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 2, "extremely high");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 2, "considerable");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 2, "moderate");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 2, "negligible");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 3, "wealth");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 3, "level of unrest");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 3, "indifference");
            raceAttributes.Rows.Add(Guid.NewGuid(), StoryFragmentType.Race, StoryFragmentContext.Politics, StoryFragmentUsage.Adjective, 3, "tendency for revolution");
*/