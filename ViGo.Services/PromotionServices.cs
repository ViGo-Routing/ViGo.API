//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using ViGo.Domain;
//using ViGo.Domain.Enumerations;
//using ViGo.Models.Promotions;
//using ViGo.Models.QueryString;
//using ViGo.Models.QueryString.Pagination;
//using ViGo.Repository.Core;
//using ViGo.Services.Core;
//using ViGo.Utilities.Validator;

//namespace ViGo.Services
//{
//    public class PromotionServices : BaseServices
//    {
//        public PromotionServices(IUnitOfWork work, ILogger logger) : base(work, logger)
//        {
//        }

//        public async Task<IPagedEnumerable<PromotionViewModel>> GetPromotionsAsync(
//            Guid? eventId,
//            PaginationParameter pagination, PromotionSortingParameters sorting,
//            HttpContext context, CancellationToken cancellationToken)
//        {
//            IEnumerable<Promotion>? promotions = null;
//            if (eventId is null)
//            {
//                promotions = await work.Promotions.GetAllAsync(
//                    cancellationToken: cancellationToken);
//            }
//            else
//            {
//                // Get by event
//                promotions = await work.Promotions.GetAllAsync(query => query.Where(
//                    p => p.EventId.HasValue && p.EventId.Value.Equals(eventId.Value)),
//                cancellationToken: cancellationToken);
//            }

//            promotions = promotions.Sort(sorting.OrderBy);

//            int totalRecords = promotions.Count();
//            promotions = promotions.ToPagedEnumerable(pagination.PageNumber,
//                pagination.PageSize).Data;

//            IEnumerable<Guid> eventIds = promotions.Where(
//                p => p.EventId.HasValue).Select(p => p.EventId.Value).Distinct();
//            IEnumerable<Event> events = await work.Events.GetAllAsync(
//                query => query.Where(e => eventIds.Contains(e.Id)),
//                cancellationToken: cancellationToken);

//            IEnumerable<Guid> vehicleTypeIds = promotions.Select(p => p.VehicleTypeId).Distinct();
//            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes
//                .GetAllAsync(query => query.Where(vt => vehicleTypeIds.Contains(vt.Id)),
//                cancellationToken: cancellationToken);

//            IList<PromotionViewModel> models = new List<PromotionViewModel>();
//            foreach (Promotion promotion in promotions)
//            {
//                VehicleType vehicleType = vehicleTypes.SingleOrDefault(
//                    vt => vt.Id.Equals(promotion.VehicleTypeId));

//                Event? promotionEvent = null;
//                if (promotion.EventId.HasValue)
//                {
//                    promotionEvent = events.SingleOrDefault(e => e.Id.Equals(promotion.EventId));
//                }
//                models.Add(new PromotionViewModel(promotion, vehicleType, promotionEvent));
//            }

//            return models.ToPagedEnumerable(pagination.PageNumber,
//                pagination.PageSize, totalRecords, context);
//        }

//        public async Task<PromotionViewModel> GetPromotionAsync(
//            Guid promotionId, CancellationToken cancellationToken)
//        {
//            Promotion? promotion = await work.Promotions.GetAsync(promotionId,
//                cancellationToken: cancellationToken);

//            if (promotion is null)
//            {
//                throw new ApplicationException("Khuyến mãi không tồn tại!!");
//            }

//            VehicleType vehicleType = await work.VehicleTypes.GetAsync(promotion.VehicleTypeId,
//                cancellationToken: cancellationToken);

//            Event? promotionEvent = null;
//            if (promotion.EventId.HasValue)
//            {
//                promotionEvent = await work.Events.GetAsync(promotion.EventId.Value,
//                    cancellationToken: cancellationToken);
//            }

//            return new PromotionViewModel(promotion, vehicleType, promotionEvent);
//        }

//        public async Task<Promotion> CreatePromotionAsync(PromotionCreateModel model,
//            CancellationToken cancellationToken)
//        {
//            // Check for Code
//            model.Code.StringValidate(
//                allowEmpty: false,
//                emptyErrorMessage: "Mã giảm giá không được bỏ trống!!",
//                minLength: 3,
//                minLengthErrorMessage: "Mã giảm giá phải có từ 3 kí tự trở lên!",
//                maxLength: 15,
//                maxLengthErrorMessage: "Mã giảm giá không được vượt quá 15 kí tự!!");

//            model.Code = model.Code.ToUpper();
//            Promotion codeCheckPromotion = await work.Promotions.GetAsync(
//                p => p.Code.ToUpper().Equals(model.Code), cancellationToken: cancellationToken);

//            if (codeCheckPromotion != null)
//            {
//                throw new ApplicationException("Code mã giảm giá đã tồn tại!!");
//            }

//            // Check for Name, Description
//            model.Name.StringValidate(
//                allowEmpty: false,
//                emptyErrorMessage: "Tên mã giảm giá không được bỏ trống!",
//                minLength: 5,
//                minLengthErrorMessage: "Tên mã giảm giá phải có từ 5 kí tự trở lên!",
//                maxLength: 50,
//                maxLengthErrorMessage: "Tên mã giảm giá không được vượt quá 50 kí tự!!");

//            model.Description.StringValidate(
//                allowEmpty: false,
//                emptyErrorMessage: "Mô tả mã giảm giá không được bỏ trống!",
//                minLength: 5,
//                minLengthErrorMessage: "Mô tả mã giảm giá phải có từ 5 kí tự trở lên!",
//                maxLength: 500,
//                maxLengthErrorMessage: "Mô tả mã giảm giá không được vượt quá 50 kí tự!!");

//            // Check for Discount amout and percentage
//            if (model.DiscountAmount <= 0)
//            {
//                throw new ApplicationException("Giá trị giảm giá phải lớn hơn 0!");
//            }
//            if (model.IsPercentage)
//            {
//                if (model.DiscountAmount > 100)
//                {
//                    throw new ApplicationException("Giá trị giảm giá phải nhỏ hơn 100%!");
//                }
//            }

//            // MaxDecrease
//            if (model.MaxDecrease <= 0)
//            {
//                throw new ApplicationException("Giá trị giảm giá tối đa phải lớn hơn 0!");
//            }

//            // StartTime and ExpireTime
//            if (model.ExpireTime.HasValue)
//            {
//                if (model.StartTime > model.ExpireTime.Value)
//                {
//                    throw new ApplicationException("Ngày bắt đầu và ngày hết hạn không hợp lệ!!");
//                }
//            }

//            // MaxTotalUsage
//            if (model.MaxTotalUsage.HasValue)
//            {
//                if (model.MaxTotalUsage.Value <= 0)
//                {
//                    throw new ApplicationException("Số lượt sử dụng tối đa phải lớn hơn 0!!");
//                }
//            }

//            // MinTotalPrice
//            if (model.MinTotalPrice.HasValue)
//            {
//                if (model.MinTotalPrice.Value <= 0)
//                {
//                    throw new ApplicationException("Giá trị nhỏ nhất để áp dụng mã giảm giá phải lớn hơn 0!");
//                }
//            }

//            // UsagePerUser
//            if (model.UsagePerUser.HasValue)
//            {
//                if (model.UsagePerUser.Value <= 0)
//                {
//                    throw new ApplicationException("Số lượt sử dụng tối đa cho mỗi người dùng phải lớn hơn 0!");
//                }
//            }

//            // VehicleType
//            VehicleType? vehicleType = await work.VehicleTypes.GetAsync(model.VehicleTypeId,
//                 cancellationToken: cancellationToken);
//            if (vehicleType is null)
//            {
//                throw new ApplicationException("Loại phương tiện áp dụng cho mã giảm giá không tồn tại!!");
//            }

//            // Event
//            if (model.EventId.HasValue)
//            {
//                Event? promotionEvent = await work.Events.GetAsync(model.EventId.Value,
//                    cancellationToken: cancellationToken);
//                if (promotionEvent is null)
//                {
//                    throw new ApplicationException("Sự kiện áp dụng cho mã giảm giá không tồn tại!!");
//                }
//            }

//            Promotion promotion = new Promotion
//            {
//                Code = model.Code,
//                EventId = model.EventId,
//                Name = model.Name,
//                Description = model.Description,
//                DiscountAmount = model.DiscountAmount,
//                IsPercentage = model.IsPercentage,
//                MaxDecrease = model.MaxDecrease,
//                StartTime = model.StartTime,
//                ExpireTime = model.ExpireTime,
//                TotalUsage = 0,
//                MaxTotalUsage = model.MaxTotalUsage,
//                UsagePerUser = model.UsagePerUser,
//                MinTotalPrice = model.MinTotalPrice,
//                VehicleTypeId = model.VehicleTypeId,
//                Status = PromotionStatus.AVAILABLE,
//            };

//            await work.Promotions.InsertAsync(promotion, cancellationToken: cancellationToken);
//            await work.SaveChangesAsync(cancellationToken);

//            return promotion;
//        }

//        public async Task<Promotion> UpdatePromotionAsync(PromotionUpdateModel model,
//            CancellationToken cancellationToken)
//        {
//            Promotion? promotion = await work.Promotions.GetAsync(
//                model.Id, cancellationToken: cancellationToken);

//            if (promotion is null)
//            {
//                throw new ApplicationException("Mã giảm giá không tồn tại!!");
//            }

//            // Check for Name, Description
//            if (model.Name != null && !string.IsNullOrEmpty(model.Name))
//            {
//                model.Name.StringValidate(
//                    allowEmpty: false,
//                    emptyErrorMessage: "Tên mã giảm giá không được bỏ trống!",
//                    minLength: 5,
//                    minLengthErrorMessage: "Tên mã giảm giá phải có từ 5 kí tự trở lên!",
//                    maxLength: 50,
//                    maxLengthErrorMessage: "Tên mã giảm giá không được vượt quá 50 kí tự!!");

//                promotion.Name = model.Name;
//            }

//            if (model.Description != null && !string.IsNullOrEmpty(model.Description))
//            {
//                model.Description.StringValidate(
//                    allowEmpty: false,
//                    emptyErrorMessage: "Mô tả mã giảm giá không được bỏ trống!",
//                    minLength: 5,
//                    minLengthErrorMessage: "Mô tả mã giảm giá phải có từ 5 kí tự trở lên!",
//                    maxLength: 500,
//                    maxLengthErrorMessage: "Mô tả mã giảm giá không được vượt quá 50 kí tự!!");

//                promotion.Description = model.Description;
//            }

//            // Check for Discount amout and percentage
//            if (model.DiscountAmount.HasValue)
//            {
//                if (model.DiscountAmount <= 0)
//                {
//                    throw new ApplicationException("Giá trị giảm giá phải lớn hơn 0!");
//                }

//                promotion.DiscountAmount = model.DiscountAmount.Value;
//            }

//            if (model.IsPercentage.HasValue)
//            {
//                if (model.IsPercentage.Value)
//                {
//                    if (model.DiscountAmount.HasValue)
//                    {
//                        if (model.DiscountAmount > 100)
//                        {
//                            throw new ApplicationException("Giá trị giảm giá phải nhỏ hơn 100%!");
//                        }
//                    }
//                    else
//                    {
//                        if (promotion.DiscountAmount > 100)
//                        {
//                            throw new ApplicationException("Giá trị giảm giá phải nhỏ hơn 100%!");
//                        }
//                    }
//                }

//                promotion.IsPercentage = model.IsPercentage.Value;
//            }

//            // MaxDecrease
//            if (model.MaxDecrease.HasValue)
//            {
//                if (model.MaxDecrease <= 0)
//                {
//                    throw new ApplicationException("Giá trị giảm giá tối đa phải lớn hơn 0!");
//                }

//                promotion.MaxDecrease = model.MaxDecrease.Value;
//            }

//            // StartTime and ExpireTime
//            if (model.StartTime.HasValue)
//            {
//                promotion.StartTime = model.StartTime.Value;
//            }

//            if (model.ExpireTime.HasValue)
//            {
//                if (model.StartTime.HasValue)
//                {
//                    if (model.StartTime.Value > model.ExpireTime.Value)
//                    {
//                        throw new ApplicationException("Ngày bắt đầu và ngày hết hạn không hợp lệ!!");
//                    }
//                }
//                else
//                {
//                    if (promotion.StartTime > model.ExpireTime.Value)
//                    {
//                        throw new ApplicationException("Ngày bắt đầu và ngày hết hạn không hợp lệ!!");
//                    }
//                }

//                promotion.ExpireTime = model.ExpireTime;

//            }

//            // MaxTotalUsage
//            if (model.MaxTotalUsage.HasValue)
//            {
//                if (model.MaxTotalUsage.Value <= 0)
//                {
//                    throw new ApplicationException("Số lượt sử dụng tối đa phải lớn hơn 0!!");
//                }

//                promotion.MaxTotalUsage = model.MaxTotalUsage.Value;
//            }

//            // MinTotalPrice
//            if (model.MinTotalPrice.HasValue)
//            {
//                if (model.MinTotalPrice.Value <= 0)
//                {
//                    throw new ApplicationException("Giá trị nhỏ nhất để áp dụng mã giảm giá phải lớn hơn 0!");
//                }

//                promotion.MinTotalPrice = model.MinTotalPrice.Value;
//            }

//            // UsagePerUser
//            if (model.UsagePerUser.HasValue)
//            {
//                if (model.UsagePerUser.Value <= 0)
//                {
//                    throw new ApplicationException("Số lượt sử dụng tối đa cho mỗi người dùng phải lớn hơn 0!");
//                }

//                promotion.UsagePerUser = model.UsagePerUser.Value;
//            }

//            // VehicleType
//            if (model.VehicleTypeId.HasValue)
//            {
//                VehicleType? vehicleType = await work.VehicleTypes.GetAsync(model.VehicleTypeId.Value,
//                 cancellationToken: cancellationToken);
//                if (vehicleType is null)
//                {
//                    throw new ApplicationException("Loại phương tiện áp dụng cho mã giảm giá không tồn tại!!");
//                }

//                promotion.VehicleTypeId = model.VehicleTypeId.Value;
//            }


//            // Event
//            if (model.EventId.HasValue)
//            {
//                Event? promotionEvent = await work.Events.GetAsync(model.EventId.Value,
//                    cancellationToken: cancellationToken);
//                if (promotionEvent is null)
//                {
//                    throw new ApplicationException("Sự kiện áp dụng cho mã giảm giá không tồn tại!!");
//                }

//                promotion.EventId = model.EventId.Value;
//            }

//            // Status
//            if (model.Status.HasValue)
//            {
//                if (!Enum.IsDefined(model.Status.Value))
//                {
//                    throw new ApplicationException("Trạng thái mã giảm giá không hợp lệ!!");
//                }

//                promotion.Status = model.Status.Value;
//            }

//            await work.Promotions.UpdateAsync(promotion);
//            await work.SaveChangesAsync(cancellationToken);

//            return promotion;
//        }

//        public async Task<Promotion> DeletePromotionAsync(Guid promotionId,
//            CancellationToken cancellationToken)
//        {
//            Promotion? promotion = await work.Promotions.GetAsync(
//                promotionId, cancellationToken: cancellationToken);

//            if (promotion is null)
//            {
//                throw new ApplicationException("Mã giảm giá không tồn tại!!");
//            }

//            await work.Promotions.DeleteAsync(promotion);
//            await work.SaveChangesAsync(cancellationToken);

//            return promotion;
//        }
//    }
//}
