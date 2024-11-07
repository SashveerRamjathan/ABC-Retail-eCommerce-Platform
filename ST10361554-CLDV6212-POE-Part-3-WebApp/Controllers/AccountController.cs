using Microsoft.AspNetCore.Mvc;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Interfaces;
using ST10361554_CLDV6212_POE_Part_3_WebApp.Services;
using ST10361554_CLDV6212_POE_Part_3_WebApp.ViewModels;
using System.Security.Claims;

namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Controllers
{
    public class AccountController : Controller
    {
        // Private fields for services and logger
        private readonly IAccountDatabaseService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly QueueService _queueService;
        private readonly string queueName = "account-queue";

        // Constructor to initialize services and logger
        public AccountController(
            IAccountDatabaseService service,
            ILogger<AccountController> logger,
            IQueueServiceFactory queueServiceFactory)
        {
            _accountService = service;
            _logger = logger;
            _queueService = queueServiceFactory.Create(queueName);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Display error messages
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Check if the model state is valid
            if (ModelState.IsValid)
            {
                // Validate user credentials
                var (isValid, role) = await _accountService.ValidateUserAsync(model.Email, model.Password);

                // If valid, sign in the user and log the event
                if (isValid == true && !string.IsNullOrEmpty(role))
                {
                    await _accountService.SignInAsync(model.Email);
                    _logger.LogInformation($"User {model.Email} logged in");
                    await _queueService.SendMessageAsync($"User {model.Email} logged in at {DateTime.Now.ToString()}", queueName);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // If invalid, set an error message
                    TempData["ErrorMessage"] = "We could not locate your account... \n Please check your email and password";
                }
            }

            // Redirect to the login page if the model state is invalid
            return RedirectToAction();
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Check if the model state is valid
            if (ModelState.IsValid)
            {
                // Register the user
                var result = await _accountService.RegisterUserAsync(model.Password, model.Email, "Customer", model.Name, model.StreetAddress,
                    model.City, model.Province, model.PostalCode, model.Country, model.PhoneNumber);

                // If registration is successful, log the event
                if (result)
                {
                    _logger.LogInformation($"User {model.Email} registered successfully");
                    await _queueService.SendMessageAsync($"User {model.Email} registered at {DateTime.Now.ToString()}", queueName);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // If registration fails, add an error message
                    ModelState.AddModelError(string.Empty, "User already exists");
                }
            }

            // Return the registration view if the model state is invalid
            return View();
        }

        // POST: Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await _accountService.SignOutAsync();

            // Get the current user's username before they logout
            var username = User.FindFirstValue(ClaimTypes.Name);

            // Log the logout event
            await _queueService.SendMessageAsync($"User {username} logged out at {DateTime.Now.ToString()}", queueName);
            _logger.LogInformation($"User {username} logged out");

            // Redirect to the login page
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
