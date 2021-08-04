using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SehirRehberi.Api.Data;
using SehirRehberi.Api.Dtos;
using SehirRehberi.Api.Helpers;
using SehirRehberi.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SehirRehberi.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/cities/{id}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private IAppRepository _appRepository;
        private IMapper _mapper;
        private IOptions<CloudinarySettings> _cloduinaryConfig;

        private Cloudinary _cloudinary;

        public PhotosController(IOptions<CloudinarySettings> cloduinaryConfig, IMapper mapper, IAppRepository appRepository)
        {
            _cloduinaryConfig = cloduinaryConfig;
            _mapper = mapper;
            _appRepository = appRepository;

            Account account = new Account(
                _cloduinaryConfig.Value.CloudName,
                _cloduinaryConfig.Value.ApiKey,
                _cloduinaryConfig.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        [HttpPost]
        public ActionResult AddPhotoForCity(int id, [FromBody]PhotoForCreationDto photoForCreationDto)
        {
            var city = _appRepository.GetCityById(id);

            if (city==null)
            {
                return BadRequest("Could not find the city!");
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (currentUserId!=city.UserId)
            {
                return Unauthorized();
            }

            var file = photoForCreationDto.file;

            var uploadResult = new ImageUploadResult();
            if (file.Length>0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.Name, stream)
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);
            photo.City = city;

            if (!city.Photos.Any(m=>m.IsMain))
            {
                photo.IsMain = true;
            }
            city.Photos.Add(photo);

            if (_appRepository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto",new { id=photo.Id, photoToReturn });
            }
            return BadRequest("Could not add the photo to cloud!");
        }

        // [HttpGet("{id}", Name = "GetPhoto")]
        [HttpGet]
        [Route("GetPhoto")]
        public ActionResult GetPhoto(int id)
        {
            var photoFromDb = _appRepository.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromDb);

            return Ok(photo);
        }
    }
}
