using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Project.API.Data;
using Project.API.Dtos;
using Project.API.Helpers;
using Project.API.Models;

namespace Project.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IApplicationRepository _repo;
        private readonly DataContext _context;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public UsersController(IApplicationRepository repo, DataContext context, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _repo = repo;
            _context = context;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }


        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var usersToReturn = await _repo.GetUsers();

            return Ok(usersToReturn);
        }


        [HttpPost("addPhoto/{userId}")]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var userFromRepo = await _repo.GetUser(userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photoToCreate = new Photo
            {
                Url = photoForCreationDto.Url,
                IsMain = true,
                PublicId = photoForCreationDto.PublicId,
                UserId = userFromRepo.Id
            };

            await _context.Photos.AddAsync(photoToCreate);
            await _context.SaveChangesAsync();
            return StatusCode(201);

        }
 

        [HttpPost("addInstance/{userId}")]
        public async Task<IActionResult> AddInstanceForUser(int userId, [FromBody]InstanceForCreationDto instanceForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userFromRepo = await _repo.GetUser(userId);

            var instanceToCreate = new Instance 
            {
                Content = instanceForCreationDto.Content,
                InstanceStart = instanceForCreationDto.InstanceStart,
                InstanceEnd = instanceForCreationDto.InstanceEnd,
                TypeOfInstance = instanceForCreationDto.TypeOfInstance,
                UserId = userFromRepo.Id,
                Username = userFromRepo.Username,
                UserSurname = userFromRepo.UserSurname,
                Position = userFromRepo.Position,
                Approval = "false",
                Reason = ""
            };

            await _context.Instances.AddAsync(instanceToCreate);
            await _context.SaveChangesAsync();
            return StatusCode(201);

        }


        [HttpPost("addBill/{userId}")]
        public async Task<IActionResult> AddBill(int userId, [FromBody]BillToAddDto billToAddDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userFromRepo = await _repo.GetUser(userId);

            var billToCreate = new Bill 
            {
                Name = billToAddDto.Name,
                Salesman = billToAddDto.Salesman,
                Buyer = billToAddDto.Buyer,
                ServiceName = billToAddDto.ServiceName,
                Price = billToAddDto.Price,
                PaymentMethod = billToAddDto.PaymentMethod
            };

            await _context.Bills.AddAsync(billToCreate);
            await _context.SaveChangesAsync();
            return StatusCode(201);

        }

        [HttpGet("getInstances")]
        public async Task<IActionResult> GetInstances()
        {
            var instancesToReturn = await _repo.GetInstances();

            return Ok(instancesToReturn);
        }

        [HttpGet("getBills")]
        public async Task<IActionResult> GetBills()
        {
            var billsToReturn = await _repo.GetBills();

            return Ok(billsToReturn);
        }

        [HttpGet("getInstancesForWorker/{userId}")]
        public async Task<IActionResult> GetInstancesForWorker(int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var instancesToReturn = await _repo.GetInstancesForWorker(userId);

            return Ok(instancesToReturn);
        }


        [HttpPost("acceptInstance/{userId}")]
        public async Task<IActionResult> AcceptInstance(int userId, [FromBody]InstanceToAcceptDto instanceToAcceptDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var workersWithSamePosition = 0;

            var instancesTable = await _context.Instances.ToArrayAsync();

            Instance instanceFromRepo = await _context.Instances.FirstOrDefaultAsync(x => x.Id.Equals(instanceToAcceptDto.Id));

            workersWithSamePosition = await _context.Users.CountAsync(u => u.Position == instanceFromRepo.Position);

            for(int i = 0; i < instancesTable.Length; i++)
            {

                if (instancesTable[i].Position == instanceFromRepo.Position && instancesTable[i].Approval == "true")
                    workersWithSamePosition--;

            };

            if (workersWithSamePosition > 2)
            {
                
            var instanceToAccept = new Instance {
                Approval = instanceToAcceptDto.Approval,
                Reason = ""
            };

            instanceFromRepo.Approval = instanceToAccept.Approval;
            instanceFromRepo.Reason = instanceToAccept.Reason;

            _context.Entry(instanceFromRepo).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            } else {
                return BadRequest();
            };

            return StatusCode(201);  
        }


        [HttpPost("disapprovalInstance/{userId}")]
        public async Task<IActionResult> DisapprovalInstance(int userId, [FromBody]InstanceToDisapprovalDto instanceToDisapprovalDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Instance instanceFromRepo = await _context.Instances.FirstOrDefaultAsync(x => x.Id.Equals(instanceToDisapprovalDto.Id));

            var instanceToDisapproval = new Instance {
                Approval = instanceToDisapprovalDto.Approval,
                Reason = instanceToDisapprovalDto.Reason
            };

            instanceFromRepo.Approval = instanceToDisapproval.Approval;
            instanceFromRepo.Reason = instanceToDisapproval.Reason;

            _context.Entry(instanceFromRepo).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return StatusCode(201);  
        }
        
    }
}
