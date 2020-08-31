export interface ICardResponse {
    items: ICard[];
    total: number;
    itemsPerPage: number;
    filterTags: ITag[];
    //mainTags: ITag[];
    //subTags: ITag[];
}

export const DefaultICardResponse: ICardResponse = { items:[], itemsPerPage:0, total:0, filterTags:[] }; // , mainTags:[], subTags:[]

export interface ICard {
    id: number;
    header: string;
    description: string;
    area: string;
    city: string;
    onlineOnly: boolean;
    hasImage: boolean;
    imageUrl: string;
    imageHtml: string;
    imageAlt: string;
    tags: string[];
    detailPageLink: string;
    addressAndCoordinates: ICardAddressAndCoordinate[];
}

export interface ICardCoordinate {
    id: number;
    detailPageLink: string;
    header: string;
    description: string;
    addressAndCoordinates: ICardAddressAndCoordinate[];
}

export interface ICardAddressAndCoordinate {
    latitude: number;
    longitude: number;
    address: string;
}

export interface ITag {
    id: number;
    name: string;
    count: number;
    type: string;
    checked: boolean;
}

export interface ITranslation {
    key: string;
    value: string;
}