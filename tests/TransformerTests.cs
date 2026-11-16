using System;
namespace YodaFy
{
    static class TransformerTests
    {
        public static int Run()
        {
            string[,] cases = {
                {"I am ready.", "Ready, I am."},
                {"You will learn the Force!", "Learn the Force, you will!"},
                {"I love pizza", "Pizza, I love."},
                {"I don't like this.", "Like this, I do not."},
                {"I'm ready", "Ready, I am."},
                {"The Jedi are very strong.", "Very strong, the Jedi are."},
                {"Luke is my friend.", "My friend, Luke is."},
                {"I can visit https://example.com/a?q=1.", "Visit https://example.com/a?q=1, I can."},
                {"I can visit <@12345>.", "Visit <@12345>, I can."},
                {"I can visit [the site](https://example.com).", "Visit [the site](https://example.com), I can."},
                {"I am ready.  We are happy.\r\nHello there!", "Ready, I am.  Happy, we are.\r\nHello there!"},
                {"Can you help me?", "Can you help me?"},
                {"Ready, I am.", "Ready, I am."},
                {"\u4F60\u597D \uD83C\uDF0E", "\u4F60\u597D \uD83C\uDF0E"},
                {"```js\nI am code\n```\nI am ready", "```js\nI am code\n```\nReady, I am."},
                {"I am using `code`", "I am using `code`"},
                {"/yoda I am ready", "/yoda I am ready"},
                {"  I am happy  ", "  Happy, I am.  "},
                {"", ""}, {"   ", "   "}, {"I am \uE0000\uE001", "I am \uE0000\uE001"}
            };
            for (int i = 0; i < cases.GetLength(0); i++)
            {
                string output = YodaTransformer.Transform(cases[i, 0]);
                if (output != cases[i, 1]) throw new Exception("Transformer case " + i + " failed: " + output);
                if (YodaTransformer.Transform(output) != output) throw new Exception("Conversion is not idempotent: " + output);
            }
            try { YodaTransformer.Transform(null); } catch (ArgumentNullException) { return cases.GetLength(0) * 2 + 1; }
            throw new Exception("Transformer accepted null");
        }
    }
}
