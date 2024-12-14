using System.CommandLine;
using System.Text;
using static System.CommandLine.Help.HelpBuilder;

//root command
var rootCommand = new RootCommand("Bundle code files to a single file");

//option of bundle
var languageOption = new Option<string>("--language", () => "all", "Language of code files to bundle");
languageOption.AddAlias("-l");
var outputOption = new Option<FileInfo>("--output", "Output file name");
outputOption.AddAlias("-o");
var sortOption = new Option<string>("--sort", () => "filename", "Sort files by name");
sortOption.AddAlias("-s");
var noteOption = new Option<bool>("--note", () => false, "Write source files");
noteOption.AddAlias("-n");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", () => false, "Remove empty lines");
removeEmptyLinesOption.AddAlias("-r");
var authorOption = new Option<string>("--author", () => "", "Author name");
authorOption.AddAlias("-a");

//create command with options
var bundleCommand = new Command("bundle", "bundle code files to a single file")
            {
                languageOption,
                outputOption,
                sortOption,
                noteOption,
                removeEmptyLinesOption,
                authorOption
            };

bundleCommand.SetHandler((language, output, sort, note, removeEmptyLines, author) =>
{
    try 
    { 
        BundleFiles(language, output.FullName, sort, note, removeEmptyLines, author);
        Console.WriteLine("File was created");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"error: {ex.Message}");
    }
}, languageOption, outputOption, sortOption, noteOption, removeEmptyLinesOption, authorOption);

static string PromptUser(string prompt)
{
    Console.Write($"{prompt}: ");
    return Console.ReadLine();
}
static bool PromptUserBool(string prompt)
{
    Console.Write($"{prompt}: ");
    return Console.ReadLine().Equals("true");
}

var createRspCommand = new Command("create-rsp", "Create a response file for bundling");
createRspCommand.SetHandler(() =>
{
    string language = PromptUser("Enter language (all for all languages)");
    string outputPath = PromptUser("Enter output file path");
    string sort = PromptUser("Sort files (filename/extension)");
    bool note = PromptUserBool("Add notes to bundled file (true/false)");
    bool removeEmptyLines = PromptUserBool("Remove empty lines (true/false)");
    string author = PromptUser("Enter author name");

    // יצירת קובץ התגובה
    string rspFile = "bundle.rsp";
    using (StreamWriter writer = new StreamWriter(rspFile))
    {
        writer.WriteLine($"bundle --language \"{language}\" --output \"{outputPath}\" --sort \"{sort}\" --note {note} --remove-empty-lines {removeEmptyLines} --author \"{author}\"");
    }

    Console.WriteLine($"Response file '{rspFile}' created successfully.");
});

rootCommand.AddCommand(createRspCommand);
rootCommand.AddCommand(bundleCommand);
rootCommand.InvokeAsync(args);

void BundleFiles(string languages, string outputPath, string sort, bool note, bool removeEmptyLines, string author)
{
    try
    {
        // Validate languages
        if (string.IsNullOrEmpty(languages))
        {
            throw new ArgumentException("must be value in language.");
        }
        var extensions = languages.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var excludedDirectories = new string[] { "bin", "debug", "obj" };
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
                             .Where(file => !excludedDirectories.Any(dir => file.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), dir), StringComparison.OrdinalIgnoreCase)))
                             .ToArray();
        // If language is not "all", filter files based on extensions (optional)
        if (!languages.Equals("all"))
        {
            files = files.Where(file => extensions.Any(ext =>
                Path.GetExtension(file).Equals(ext, StringComparison.OrdinalIgnoreCase))).ToArray();
        }
        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("must be value in output.");
        }

        // Validate sort
        if (!sort.Equals("filename", StringComparison.OrdinalIgnoreCase) && !sort.Equals("extension", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(" sort must be: 'filename' or 'extension'.");
        }
        else if(sort.Equals("filename"))
            files = files.OrderBy(file => file).ToArray();

        else // Sort by extension (ascending)
        {
            files = files.OrderBy(file => Path.GetExtension(file)).ToArray();
        }
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"Author: {author}");
            }

            foreach (var file in files)
            {
                string content = File.ReadAllText(file);

                if (note)
                {
                    // מקבל את הניתוב היחסי ביחס לתיקייה הנוכחית
                    string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    content = $"// From: {relativePath}{Environment.NewLine}{content}";
                }

                if (removeEmptyLines)
                {
                    content = string.Join(Environment.NewLine, content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
                }

                writer.Write(content); // Write each file's content directly
            }
        }
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($" boolean error: {ex.Message}");
    }
    catch (IOException ex)
    {
        Console.WriteLine($" i/o error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"error: {ex.Message}");
    }
}

