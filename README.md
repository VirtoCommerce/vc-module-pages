# Virto Pages Documentation

## Overview

Virto Pages is a module designed to manage and display content pages within the Virto Commerce platform. It allows you to store, search, and retrieve pages from supported Content Management Systems (CMS). The module integrates seamlessly with existing stores, enhancing their capabilities for content management.



## Features

- **Storing Content Pages**: Save pages retrieved from a CMS into the Virto platform for efficient management.
- **Page Search**: Find pages quickly using advanced search functionality.
- **Retrieve Pages by ID or Permalink**: Access pages easily using unique identifiers or user-friendly URLs.

## User Guide

To enable Virto Pages for your store, follow these steps:

1. Go to the **Stores** section in the platform.
2. Select the desired store.
3. In the **Settings** widget, navigate to the `Virto Pages` section and toggle the **Enabled** setting to "On".



Once enabled, the module will be active for that specific store, allowing you to manage pages directly.

## Module Workflow

### Under the Hood

The Virto Pages module operates through integration with your selected CMS, leveraging hooks and events to manage content updates efficiently.

- **Content Retrieval**: The integration module receives pages from the CMS through a dedicated hook, which is responsible for fetching the requested page.
- **Conversion to PageDocument**: The fetched page is then converted into a `PageDocument` model ([see documentation](link)).
- **Event Handling**: The module triggers a `PagesDomainEvent` ([see documentation](link)), which is processed by the pages module. During processing, the document can either be indexed for searching or removed from the index as needed.

This event-driven architecture ensures that the content in your store is always up to date and can be easily managed.

### API Endpoints

The module provides a single endpoint for searching pages:

```api
POST /api/pages/search
```

#### Request Example

```json
{
  "storeId": "string",
  "permalink": "string",
  "visibility": "Private",
  "status": "Draft",
  "certainDate": "2024-11-20T11:34:37.396Z",
  "userGroups": [
    "string"
  ],
  "responseGroup": "string",
  "objectType": "string",
  "objectTypes": [
    "string"
  ],
  "objectIds": [
    "string"
  ],
  "keyword": "string",
  "searchPhrase": "string",
  "languageCode": "string",
  "sort": "string",
  "skip": 0,
  "take": 0
}
```

#### Response Example

```json
{
  "totalCount": 0,
  "results": [
    {
      "id": "string",
      "outerId": "string",
      "storeId": "string",
      "cultureName": "string",
      "permalink": "string",
      "title": "string",
      "description": "string",
      "status": "Draft",
      "createdDate": "2024-11-20T11:34:37.405Z",
      "modifiedDate": "2024-11-20T11:34:37.405Z",
      "createdBy": "string",
      "modifiedBy": "string",
      "source": "string",
      "mimeType": "string",
      "content": "string",
      "visibility": "Private",
      "userGroups": [
        "string"
      ],
      "startDate": "2024-11-20T11:34:37.405Z",
      "endDate": "2024-11-20T11:34:37.405Z"
    }
  ]
}
```

#### Example Usage

To search for pages in a specific store, use the following request format:

```bash
curl -X POST "https://example.com/api/pages/search" \
  -H "Content-Type: application/json" \
  -d '{
    "storeId": "store123",
    "keyword": "homepage",
    "visibility": "Public",
    "status": "Published",
    "take": 10
  }'
```

The response will contain a list of pages that match the search criteria.

### GraphQL Queries

The Virto Pages module provides several GraphQL queries to interact with the content effectively.

#### Querying Page Information by Permalink

To determine whether a given permalink corresponds to an existing page, use the `slugInfo` query:

```graphql
query {
  slugInfo(
    permalink: "/test-2"
    storeId: "B2B-store"
    cultureName: "en-US"
  ) {
    entityInfo {
      id
      isActive
      languageCode
      objectId
      objectType
      semanticUrl
      __typename
    }
    __typename
  }
}
```

#### Example Response

The response for the `slugInfo` query provides detailed information about the entity corresponding to the permalink:

```json
{
  "data": {
    "slugInfo": {
      "entityInfo": {
        "id": "24caa0d5a05145f3a3433a2930fbfb0f",
        "isActive": true,
        "languageCode": "en-US",
        "objectId": "24caa0d5a05145f3a3433a2930fbfb0f",
        "objectType": "Pages",
        "semanticUrl": "/test-2",
        "__typename": "SeoInfo"
      },
      "__typename": "SlugInfoResponseType"
    }
  }
}
```

#### Downloading Page Content by ID

To retrieve the content of a specific page by its ID, use the `pageDocument` query:

```graphql
query {
  pageDocument(id: "24caa0d5a05145f3a3433a2930fbfb0f") {
    id
    source
    permalink
    content
  }
}
```

#### Example Response

```json
{
  "data": {
    "pageDocument": {
      "id": "24caa0d5a05145f3a3433a2930fbfb0f",
      "source": "builder.io",
      "permalink": "/test-2",
      "content": "..."
    }
  }
}
```

#### Searching Pages by Keywords

You can search for pages based on specific keywords using the `pageDocuments` query:

```graphql
query {
  pageDocuments(after: "0", first: 10, storeId: "B2B-store", keyword: "tv", cultureName: "en-US") {
    totalCount
    items {
      id
      source
      permalink
      content
    }
  }
}
```

#### Example Response

The response for the `pageDocuments` query includes a list of pages matching the search criteria:

```json
{
  "data": {
    "pageDocuments": {
      "totalCount": 16,
      "items": [
        {
          "id": "9911349254704e3596ddbe9136fc8273",
          "source": "builder.io",
          "permalink": "/televisions-d",
          "content": "..."
        }
        // Additional pages...
      ]
    }
  }
}
```

The `source` property indicates the CMS used to create the page.

## Supported CMS Platforms

- **BuilderIO**: Fully supported for integration ([BuilderIO Documentation](link)).
- **Contentful** (coming soon).
- **Optimizely** (coming soon).

## Future Enhancements

The Virto Pages module is constantly evolving. Upcoming features include support for additional CMS platforms and enhanced search capabilities to further streamline content management.

