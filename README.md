# Virto Commerce Pages Module

The Virto Commerce Pages module is a solution designed to connect different CMS seamlessly within the Virto Commerce. It offers a CMS-agnostic architecture, enabling users to manage public, private, and personalized pages effectively. Once pages are published, they are stored within Virto Pages, making the CMS optional after the design phase. This approach allows for flexibility in detaching, replacing, or using multiple CMS platforms simultaneously for scenarios like landing pages, blogs, and more.

![image](https://github.com/user-attachments/assets/a40ae81a-6f11-4f57-971f-cd512772bd1d)


## Key features

* **CMS-Agnostic Architecture**: CMS is required only during the design phase. Once published, content is stored in Virto Pages for use without real-time CMS dependency.
* **Hosted Content Pages**: Save pages retrieved from a CMS into the Virto platform for efficient management.
* **Content Scenarios:**
  * **Public Pages**: Accessible to all users.
  * **Private Pages:** Restricted to authorized users.
  * **Personalized Pages:** Tailored content for specific user groups.
  * **Scheduled Publishing:** Define start and end dates for content visibility.
  * **Retrieve Pages by permalink**: Access pages easily using unique identifiers or user-friendly URLs.
  * **Retrieve Pages by ID**: Access pages easily using unique identifiers or user-friendly URLs.
* **Full-Text Search Capabilities:** Quickly search and retrieve pages by keyword.

## Supported CMS Platforms

* **Builder.io**: Fully supported for integration.
* **Contentful**: (coming soon).
* **Optimizely**: (coming soon).
* **Virto**: Page Builder (coming soon).

## Architecture

![image](https://github.com/user-attachments/assets/4f85edf5-c905-4761-9d64-b44b938f4c29)

The Virto Pages module employs an event-driven architecture to ensure efficient content management and retrieval. Key components include:

* **Content Storage**: Pages are stored offline in an index, ensuring quick access and rendering without live CMS dependency.
* **Event Handling**: Changes in content trigger events to update, index, or remove pages as necessary.
* **Frontend Integration**: Pages are resolved their permalink (slug) from the offline index for rendering.
* **Unified Page Document and API**: Unified Page Document and API allow access to pages that are created by different CMS.

Scenarios:

* **Design Time**:
  * Integrate with a CMS to create and design pages.
  * Configure and prepare content for publishing.
* **Publishing**:
  * Publish pages to store them in Virto Pages.
  * CMS is no longer required post-publishing.
* **Rendering**:
  * Pages are accessed and rendered via their permalink or unique ID.
  * Content is served from offline storage for fast and reliable performance.
  * Customer can search pages by keyword.

## User guide

To enable Virto Pages for your store, follow these steps:

1. Go to the **Stores** section in the platform.
2. Select the desired store.
3. In the **Settings** widget, navigate to the `Virto Pages` section and toggle the **Enabled** setting to "On".

![Enable Module Pages](docs/media/enable-module-pages.png)

Once enabled, the module will be active for that specific store, allowing you to manage pages directly.

![Pages list](docs/media/pages-list.png)

## Module Workflow

### Under the Hood

The Virto Pages module operates through integration with your selected CMS, leveraging hooks and events to manage content updates efficiently.

- **Content Retrieval**: The integration module receives pages from the CMS through a dedicated hook, which is responsible for fetching the requested page.
- **Conversion to PageDocument**: The fetched page is then converted into a [`PageDocument`](src/VirtoCommerce.Pages.Core/Models/PageDocument.cs) model.
- **Event Handling**: The module triggers a [`PagesDomainEvent`](src/VirtoCommerce.Pages.Data/Handlers/PageChangedHandler.cs), which is processed by the pages module. During processing, the document can either be indexed for searching or removed from the index as needed.

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

## Documentation

* [Pages module user documentation](https://docs.virtocommerce.org/platform/user-guide/pages/overview/)
* [GraphQL API documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/Pages/overview/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Pages)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-pages)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-pages/releases/latest)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.

