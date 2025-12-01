# Category Browsing Feature - Testing Guide

## Overview
This document describes how to test the category browsing feature that allows buyers to browse products by category.

## Feature Components

### 1. Homepage (`/`)
- Displays all active root-level categories
- Each category card shows:
  - Category name
  - Number of products
  - Number of subcategories
- Clicking a category navigates to the category page

### 2. Category Page (`/category/{id}`)
- Shows category name in breadcrumb navigation
- Displays subcategories (if available) with navigation buttons
- Lists all products in the category and its subcategories
- Supports pagination (12 products per page)
- Shows empty state when no products exist
- Returns 404 for non-existent or inactive categories

## Manual Testing Steps

### Prerequisites
1. Start the application: `dotnet run`
2. Application runs at `http://localhost:5000`

### Step 1: Register Admin User
1. Navigate to `/Account/Register`
2. Fill in registration form:
   - Select "Buyer" account type (you can change to Admin later in the database)
   - Provide email, password, and required fields
3. After registration, you'll need to manually update the user role in the database to Admin

### Step 2: Create Categories
1. Login as admin user
2. Navigate to `/Admin/Categories`
3. Create root categories:
   - Click "Create New Category"
   - Name: "Electronics", Display Order: 1
   - Click "Create Category"
   - Repeat for "Clothing" with Display Order: 2
4. Create subcategories:
   - For Electronics: Create "Laptops" and "Phones" as subcategories
   - For Clothing: Create "Men" and "Women" as subcategories

### Step 3: Register Seller and Create Products
1. Register a new seller account
2. Complete seller onboarding
3. Create products in the Seller panel:
   - Navigate to Seller > Products > Create
   - Create products and assign them to categories
   - Ensure products have:
     - Title, Description, Price, Stock
     - Category assigned (select from dropdown)
     - Status set to "Active"
     - At least one product image

### Step 4: Test Category Browsing

#### Test Case 1: Homepage Category Display
1. Navigate to homepage (`/`)
2. Verify:
   - ✓ Categories are displayed in a grid layout
   - ✓ Each category shows product count
   - ✓ Each category shows subcategory count
   - ✓ Categories are clickable

#### Test Case 2: Browse Category with Products
1. Click on a category (e.g., "Electronics")
2. Verify:
   - ✓ Breadcrumb shows: Home > Electronics
   - ✓ Category name displayed as heading
   - ✓ Subcategories section shows "Laptops" and "Phones" buttons
   - ✓ Products are displayed in a grid (4 columns on desktop)
   - ✓ Each product card shows: image, title, store name, price, stock status
   - ✓ Product count displayed: "Showing 1 - X of Y products"

#### Test Case 3: Navigate to Subcategory
1. From Electronics page, click "Laptops" subcategory button
2. Verify:
   - ✓ URL changes to `/category/{laptops-id}`
   - ✓ Breadcrumb shows: Home > Laptops
   - ✓ Only laptop products are displayed
   - ✓ If Laptops has no subcategories, subcategory section is hidden

#### Test Case 4: Empty Category State
1. Navigate to a category with no products
2. Verify:
   - ✓ Empty state icon displayed
   - ✓ Message: "No Products Found"
   - ✓ Subcategories suggested (if available)
   - ✓ "Go to Home" button present

#### Test Case 5: Pagination
1. Create more than 12 products in a category
2. Navigate to that category page
3. Verify:
   - ✓ Only 12 products shown per page
   - ✓ Pagination info: "Showing 1 - 12 of X products"
   - ✓ Page numbers displayed at bottom
   - ✓ Previous/Next buttons work correctly
   - ✓ Current page is highlighted
   - ✓ Clicking page number loads correct products

#### Test Case 6: Product Navigation
1. From category page, click on a product card
2. Verify:
   - ✓ Navigates to product detail page (`/product/{id}`)
   - ✓ Product details are displayed correctly

#### Test Case 7: Invalid Category ID
1. Navigate to `/category/99999` (non-existent ID)
2. Verify:
   - ✓ Returns 404 error
   - ✓ Shows "Category Not Found" message
   - ✓ "Go to Home" button present

#### Test Case 8: Inactive Category
1. Create a category and set it to inactive in Admin panel
2. Navigate to that category's URL
3. Verify:
   - ✓ Returns 404 error (inactive categories hidden from buyers)

## Expected UI Screenshots

### Homepage with Categories
- Grid layout showing category cards
- Each card has category name, product count, subcategory count

### Category Page with Products
- Breadcrumb navigation
- Subcategory navigation buttons
- Product grid (4 columns)
- Pagination controls

### Empty Category State
- Centered empty state icon
- Helpful message and navigation options

## API Endpoints Used

- `GET /category/{id}?page={pageNumber}` - Category listing page
- Uses `ICategoryService.GetCategoryByIdAsync()`
- Uses `ICategoryService.GetCategoryTreeAsync()`
- Uses `ICategoryService.GetDescendantCategoryIdsAsync()`
- Uses `IProductService.GetProductsByCategoryIdsAsync()`

## Notes

- Categories are hierarchical (support parent-child relationships)
- Products assigned to a category also appear when browsing parent category
- Only active categories are visible to buyers
- Pagination defaults to 12 products per page
- SEO-friendly URLs use category ID (can be enhanced later with slugs)
