// In your Models folder, or a new Messages folder
namespace RTXLauncher.Core.Models;

// A message that will be broadcast whenever progress is updated.
// Using a record is a concise way to define a simple data carrier.
public record ProgressReportMessage(InstallProgressReport Report);

// A message that will be broadcast when packages are installed/updated
// so UI can refresh installed version displays
public record PackagesUpdatedMessage();

// A message that will be broadcast when a mod is deleted from the Installed Mods page
// so the Get Mods page can update the IsInstalled status
public record ModDeletedMessage(string ModPageUrl);