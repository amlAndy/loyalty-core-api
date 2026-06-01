# Love Brackley Loyalty API - Feature Development Roadmap

## Completed ✅
- [x] Authentication & Authorization
- [x] Member Profiles & Dashboard
- [x] Loyalty Card Management
- [x] Participating Businesses Search & Browse
- [x] Earn/Redeem Points Transactions
- [x] Business Dashboard & Staff Management
- [x] Loyalty Points Rules Configuration
- [x] Admin Dashboard & Management
- [x] Audit Logging & Review Cases

---

## Phase 1: Core Member Experience Enhancements

### 1. Member Favorite Businesses / Saved Shops
**Priority: HIGH | Complexity: MEDIUM | Dependencies: None**
- [ ] Create `MemberShopPreferenceEntity` (favorite businesses + preferences)
- [ ] Add database migration for `MemberShopPreference` table
- [ ] API: `POST /api/members/me/favorites/{businessId}` - Add favorite
- [ ] API: `DELETE /api/members/me/favorites/{businessId}` - Remove favorite
- [ ] API: `GET /api/members/me/favorites?page=1&pageSize=20` - List favorites
- [ ] API: `PUT /api/members/me/favorites/{businessId}/preferences` - Update notification settings
- [ ] Add to MemberDashboardDto (featured saved shops)
- [ ] Unit tests for favorite management
- [ ] Acceptance tests for favorite workflows

### 2. Member Notifications & Preferences
**Priority: HIGH | Complexity: MEDIUM | Dependencies: Member Favorite Businesses**
- [ ] Create `MemberNotificationPreferenceEntity`
- [ ] Create `NotificationEntity` (queue for async sending)
- [ ] Add `INotificationService` interface
- [ ] Implement push notifications for:
  - [ ] Favorite business offers/updates
  - [ ] Points expiration warnings
  - [ ] Tier/milestone achievements
  - [ ] Birthday/anniversary rewards
- [ ] API: `GET /api/members/me/notifications?page=1&pageSize=20` - Get notification history
- [ ] API: `PUT /api/members/me/notifications/{notificationId}/read` - Mark as read
- [ ] API: `PUT /api/members/me/notification-preferences` - Update preferences
- [ ] Background job for async notification sending

### 3. Member Referral Program
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Member Profiles**
- [ ] Create `MemberReferralEntity`
- [ ] Add `ReferralCode` to `MemberProfileEntity`
- [ ] API: `GET /api/members/me/referral-link` - Get unique referral link
- [ ] API: `POST /api/members/referral/{referralCode}` - Sign up via referral
- [ ] API: `GET /api/members/me/referrals?page=1` - View referral history & rewards
- [ ] Bonus points logic for successful referrals
- [ ] Database migration for referral tracking

---

## Phase 2: Business Features & Operations

### 4. Member Offers & Vouchers
**Priority: HIGH | Complexity: MEDIUM | Dependencies: Member Favorite Businesses**
- [ ] Create `BusinessOfferEntity`
- [ ] Create `MemberVoucherEntity` (claimed offers/vouchers)
- [ ] Create `OfferRedemptionEntity` (redemption tracking)
- [ ] API: `GET /api/participating-businesses/{businessId}/offers` - Browse offers
- [ ] API: `GET /api/members/me/available-offers?category=` - Personalized offers
- [ ] API: `POST /api/members/me/vouchers/{offerId}/claim` - Claim voucher
- [ ] API: `GET /api/members/me/vouchers?status=active|used|expired` - Member vouchers
- [ ] API: `POST /api/businesses/{businessId}/offers` - Create offer (owner only)
- [ ] API: `PUT /api/businesses/{businessId}/offers/{offerId}` - Update offer
- [ ] Offer expiration logic
- [ ] Voucher redemption validation

### 5. Business Locations & Multi-Location Support
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: None**
- [ ] Enhance `BusinessLocationEntity` with more details
- [ ] API: `GET /api/businesses/{businessId}/locations` - List all locations
- [ ] API: `POST /api/businesses/{businessId}/locations` - Create new location (owner)
- [ ] API: `PUT /api/businesses/{businessId}/locations/{locationId}` - Update location
- [ ] API: `DELETE /api/businesses/{businessId}/locations/{locationId}` - Delete location
- [ ] Transaction filtering by location
- [ ] Location-specific transaction reports
- [ ] Staff assignment to specific locations

### 6. Business Hours & Holiday Management
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Business Locations**
- [ ] Create `BusinessHolidayEntity`
- [ ] Create `BusinessLocationHourEntity` (detailed daily hours)
- [ ] API: `GET /api/businesses/{businessId}/hours` - Get business hours
- [ ] API: `PUT /api/businesses/{businessId}/hours` - Update hours
- [ ] API: `GET /api/businesses/{businessId}/holidays` - List holidays
- [ ] API: `POST /api/businesses/{businessId}/holidays` - Add holiday closure
- [ ] API: `DELETE /api/businesses/{businessId}/holidays/{holidayId}` - Remove holiday
- [ ] Open/closed status calculation based on hours & holidays
- [ ] Integration with business search (show open/closed status)

### 7. Business Reviews & Ratings
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Member Profiles, Business Profiles**
- [ ] Create `BusinessReviewEntity`
- [ ] API: `POST /api/businesses/{businessId}/reviews` - Submit review (members only)
- [ ] API: `GET /api/businesses/{businessId}/reviews?page=1&pageSize=20` - Get reviews
- [ ] API: `PUT /api/businesses/{businessId}/reviews/{reviewId}` - Edit own review
- [ ] API: `DELETE /api/businesses/{businessId}/reviews/{reviewId}` - Delete own review
- [ ] API: `POST /api/businesses/{businessId}/reviews/{reviewId}/helpful` - Mark helpful
- [ ] Average rating calculation
- [ ] Moderation flags for inappropriate reviews
- [ ] Admin review moderation: `PUT /api/admin/reviews/{reviewId}/status`

---

## Phase 3: Analytics & Reporting

### 8. Member Activity Analytics
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Existing transactions**
- [ ] API: `GET /api/members/me/analytics/summary` - Monthly/yearly stats
  - Total spent, points earned, points redeemed, visits
- [ ] API: `GET /api/members/me/analytics/top-businesses` - Favorite shops by visits
- [ ] API: `GET /api/members/me/analytics/spending-trends` - Spending by category/month
- [ ] API: `GET /api/members/me/analytics/rewards-impact` - Savings via rewards
- [ ] Charts/data for dashboard

### 9. Business Analytics & Reports
**Priority: HIGH | Complexity: MEDIUM | Dependencies: Existing transactions**
- [ ] API: `GET /api/businesses/{businessId}/analytics/summary` - Dashboard metrics
- [ ] API: `GET /api/businesses/{businessId}/analytics/member-segments` - Member breakdown
- [ ] API: `GET /api/businesses/{businessId}/analytics/spending-patterns` - Trends by day/time
- [ ] API: `GET /api/businesses/{businessId}/analytics/top-members` - VIP members
- [ ] API: `GET /api/businesses/{businessId}/reports/export?format=csv|excel` - Export reports
- [ ] Scheduled report generation & email delivery

### 10. Admin Analytics & System Health
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: None**
- [ ] API: `GET /api/admin/analytics/overview` - System-wide metrics
- [ ] API: `GET /api/admin/analytics/member-engagement` - Member participation
- [ ] API: `GET /api/admin/analytics/business-performance` - Business rankings
- [ ] API: `GET /api/admin/reports/transactions?dateRange=&status=` - Transaction reports
- [ ] System health dashboard
- [ ] Performance monitoring endpoints

---

## Phase 4: Advanced Features

### 11. Tiered Membership Levels
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Member Analytics**
- [ ] Create `MemberTierEntity` (Bronze, Silver, Gold, Platinum)
- [ ] Add tier-based benefits (bonus point multipliers, exclusive offers)
- [ ] API: `GET /api/members/me/tier-benefits` - View current tier & benefits
- [ ] Tier progression calculation based on annual spending
- [ ] Automatic tier upgrade/downgrade logic
- [ ] Tier-specific dashboards (progress to next tier)
- [ ] Admin tier management endpoints

### 12. Points Expiry & Cleanup
**Priority: MEDIUM | Complexity: LOW | Dependencies: None**
- [ ] Create `PointsExpiryPolicyEntity` (configurable by admin)
- [ ] Background job to mark expired points
- [ ] Notification before expiry (warning period)
- [ ] API: `GET /api/members/me/wallet/expiring-points` - Show expiring points
- [ ] Expiry audit logging
- [ ] Admin: `PUT /api/admin/points-expiry-policy` - Configure policy

### 13. Loyalty Transaction Disputes/Claims
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: Existing transactions**
- [ ] Create `DisputeEntity` & `DisputeMessageEntity` (for messaging)
- [ ] API: `POST /api/members/me/disputes` - File a dispute
- [ ] API: `GET /api/members/me/disputes?status=` - View disputes
- [ ] API: `POST /api/members/me/disputes/{disputeId}/messages` - Add message
- [ ] Admin: `GET /api/admin/disputes?status=open` - Dispute management
- [ ] Admin: `PUT /api/admin/disputes/{disputeId}/resolve` - Resolve dispute
- [ ] Automatic resolution workflow
- [ ] Points reimbursement logic

### 14. Business Performance Tiers & Incentives
**Priority: LOW | Complexity: MEDIUM | Dependencies: Business Analytics**
- [ ] Create `BusinessPerformanceTierEntity`
- [ ] Tier classification (high/medium/low performer)
- [ ] Tier-based fee structures
- [ ] Bonus payout calculations
- [ ] Marketing support allocation by tier
- [ ] Admin: `GET /api/admin/business-tiers` - Tier dashboard

### 15. Email Campaign & Marketing Automation
**Priority: LOW | Complexity: HIGH | Dependencies: Notifications, Offers**
- [ ] Create `EmailCampaignEntity` & `CampaignRecipientEntity`
- [ ] Admin: `POST /api/admin/campaigns` - Create campaign
- [ ] Admin: `PUT /api/admin/campaigns/{campaignId}` - Edit campaign
- [ ] Admin: `POST /api/admin/campaigns/{campaignId}/send` - Send campaign
- [ ] Segmentation engine (by tier, activity, preferences)
- [ ] A/B testing support
- [ ] Email template management
- [ ] Campaign analytics (open rate, click rate, conversion)
- [ ] Unsubscribe management

---

## Phase 5: Infrastructure & Support

### 16. Member Support Tickets
**Priority: MEDIUM | Complexity: MEDIUM | Dependencies: None**
- [ ] Create `SupportTicketEntity` & `TicketMessageEntity`
- [ ] API: `POST /api/members/me/support/tickets` - Create ticket
- [ ] API: `GET /api/members/me/support/tickets?status=` - View tickets
- [ ] API: `POST /api/members/me/support/tickets/{ticketId}/messages` - Add message
- [ ] Admin: `GET /api/admin/support/tickets?assigned=` - Support dashboard
- [ ] Admin: `PUT /api/admin/support/tickets/{ticketId}/assign` - Assign to staff
- [ ] Admin: `PUT /api/admin/support/tickets/{ticketId}/status` - Update status
- [ ] SLA tracking
- [ ] Auto-response templates

### 17. Knowledge Base & FAQs
**Priority: LOW | Complexity: LOW | Dependencies: None**
- [ ] Create `FaqCategoryEntity` & `FaqItemEntity`
- [ ] Public API: `GET /api/faqs?category=` - Browse FAQs
- [ ] Search FAQs: `GET /api/faqs/search?q=`
- [ ] Admin: `POST /api/admin/faqs` - Create FAQ
- [ ] Admin: `PUT /api/admin/faqs/{faqId}` - Edit FAQ
- [ ] Admin: `DELETE /api/admin/faqs/{faqId}` - Delete FAQ
- [ ] FAQ helpfulness voting

### 18. System Maintenance & Admin Tools
**Priority: LOW | Complexity: MEDIUM | Dependencies: None**
- [ ] API: `POST /api/admin/maintenance/data-cleanup` - Cleanup old records
- [ ] API: `GET /api/admin/maintenance/health-check` - System health
- [ ] API: `POST /api/admin/maintenance/backup-report` - Trigger reports
- [ ] Bulk user management
- [ ] Bulk business operations
- [ ] Data reconciliation tools

---

## Phase 6: Integrations & Expansion

### 19. Third-Party Integrations
**Priority: LOW | Complexity: HIGH | Dependencies: None**
- [ ] Stripe/Payment integration for direct redemptions
- [ ] Email service provider (SendGrid/Mailgun) upgrade
- [ ] SMS notifications via Twilio
- [ ] QR code generation for transactions
- [ ] Barcode scanning API
- [ ] Analytics integrations (Mixpanel, Amplitude)

### 20. Mobile App Support
**Priority: LOW | Complexity: MEDIUM | Dependencies: Existing APIs**
- [ ] Deep linking support
- [ ] App-specific DTOs (lightweight payloads)
- [ ] App version management
- [ ] Offline support (sync mechanism)
- [ ] Push notification integration

---

## Implementation Priority Matrix

| Feature | Phase | Priority | Complexity | Est. Effort (Days) |
|---------|-------|----------|------------|-------------------|
| Member Favorite Businesses | 1 | HIGH | MEDIUM | 3-4 |
| Member Notifications | 1 | HIGH | MEDIUM | 4-5 |
| Member Referral Program | 1 | MEDIUM | MEDIUM | 3-4 |
| Member Offers & Vouchers | 2 | HIGH | MEDIUM | 4-5 |
| Business Locations | 2 | MEDIUM | MEDIUM | 3-4 |
| Business Hours & Holidays | 2 | MEDIUM | MEDIUM | 3-4 |
| Business Reviews | 2 | MEDIUM | MEDIUM | 3-4 |
| Member Analytics | 3 | MEDIUM | MEDIUM | 3-4 |
| Business Analytics | 3 | HIGH | MEDIUM | 4-5 |
| Admin Analytics | 3 | MEDIUM | MEDIUM | 3-4 |
| Tiered Membership | 4 | MEDIUM | MEDIUM | 3-4 |
| Points Expiry | 4 | MEDIUM | LOW | 2-3 |
| Transaction Disputes | 4 | MEDIUM | MEDIUM | 3-4 |
| Business Tiers | 4 | LOW | MEDIUM | 3-4 |
| Email Campaigns | 4 | LOW | HIGH | 5-7 |
| Support Tickets | 5 | MEDIUM | MEDIUM | 3-4 |
| Knowledge Base | 5 | LOW | LOW | 2-3 |
| Admin Tools | 5 | LOW | MEDIUM | 3-4 |
| Integrations | 6 | LOW | HIGH | 5-10 |
| Mobile App | 6 | LOW | MEDIUM | 4-6 |

---

## Recommended Next Steps (Immediate)

1. **Start with Phase 1, Feature #1**: Member Favorite Businesses
   - Foundation for personalization
   - Quick win (3-4 days)
   - Enables notifications & offers features

2. **Then Phase 1, Feature #2**: Member Notifications
   - Builds on favorites
   - High engagement value
   - Requires background job infrastructure

3. **Then Phase 2, Feature #4**: Member Offers & Vouchers
   - High value for both members & businesses
   - Depends on notifications
   - Enables revenue opportunities

---

## Notes

- Each feature should include: DTOs, Controllers, Services, Entities, Migrations, Tests
- Follow the established layered architecture pattern
- Update feature-development.md after completing each phase
- Create separate branches for each feature (feature/xxx-yyy)
- Ensure backward compatibility with existing APIs
- Add logging and audit trails where appropriate
- Consider caching strategies for frequently accessed data
