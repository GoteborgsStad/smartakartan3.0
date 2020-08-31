import React, { useState, useEffect } from 'react';
import { StateService } from './stateService';
import { HttpResponse } from './http';
import { ICardResponse, ICardCoordinate, DefaultICardResponse, ITag } from './service-model';
import { IBussinessService, BussinessService } from './bussinessService';
import { Cards, DefaultSortValue } from './Components/cards';
import { Map, IMapMarker } from './Components/map';
import { SearchBar, ISearchState, ISearchStateDefaultValue } from './Components/searchBar';

interface ServiceResponse {
    service: IBussinessService | null;
    region: string;
    languageCode: string;
}

const StartPage: React.FC = () => {
    const [mapMarkers, setMapMarkers] = useState<IMapMarker[]>([]);
    const [currentPage, setCurrentPage] = useState(0);
    const [searchState, setSearchState] = useState<ISearchState>(ISearchStateDefaultValue);
    const [cardResponse, setCardResponse] = useState<ICardResponse>(DefaultICardResponse);
    const [transactionTags, setTransactionTags] = useState<ITag[]>([]);
    const [sortValue, setSortValue] = useState<string>(DefaultSortValue);
    const [filterTags, setFilterTags] = useState<ITag[]>([]);
    // const [filterMainTags, setFilterMainTags] = useState<ITag[]>([]);
    // const [filterSubTags, setFilterSubTags] = useState<ITag[]>([]);
    const [randomSeed, setRandomSeed] = useState<number>(0);

    useEffect(() => {
        queryTransactionTags();
    }, []);

    useEffect(() => {
        queryCards(sortValue, 0, getTicks());
        queryCardCoodrinates();
    }, [searchState]);

    // url: https://stackoverflow.com/a/7966778
    const getTicks = (): number => {
        return (621355968e9 + (new Date()).getTime() * 1e4);
    };

    const getService = (): ServiceResponse => {
        let baseUrl = StateService.get<string>('common.baseurl');
        let region = StateService.get<string>('common.region');
        let languageCode = StateService.get<string>('common.languagecode');

        if (region === null)
            region = '';

        if (languageCode === null)
            languageCode = '';

        let service: IBussinessService | null = null;
        if (baseUrl != null) {
            service = new BussinessService(baseUrl)
        }

        return {
            languageCode: languageCode,
            region: region,
            service: service
        };
    };

    const searchCallback = (state: ISearchState) => {
        setCurrentPage(0);
        setSearchState(state);
    };

    const paginationCallback = (page: number) => {
        setCurrentPage(page);
        queryCards(sortValue, page, randomSeed);
    };

    const sortingChange = (sorting: string) => {
        setSortValue(sorting);
        setCurrentPage(0);
        queryCards(sorting, 0, getTicks());
    };

    const queryTransactionTags = () => {
        let serviceReponse = getService();
        if (serviceReponse.service != null) {
            serviceReponse.service.getTransactionTags(serviceReponse.languageCode)
                .then((response: ITag[]) => {
                    if (response != null) {
                        response.forEach(t => t.checked = false);
                        setTransactionTags(response);
                    }
                });
        }
    };

    const queryCards = (sorting: string, page: number, randomSeed: number) => {
        let serviceReponse = getService();

        if (serviceReponse.service != null) {
            if (sorting == "Random")
                setRandomSeed(randomSeed);

            let tagString = commaSeparetedString(searchState.tags);
            let transactionTagString = commaSeparetedString(searchState.transactionTags);

            serviceReponse.service.getCards(
                searchState.query,
                tagString,
                transactionTagString,
                serviceReponse.region,
                serviceReponse.languageCode,
                page,
                searchState.digital,
                searchState.openNow,
                sorting,
                randomSeed)
                .then((response: HttpResponse<ICardResponse>) => {
                    if (response.data != null) {

                        setCardResponse({
                            items: page === 0 ? response.data.items : cardResponse.items.concat(response.data.items),
                            itemsPerPage: response.data.itemsPerPage,
                            total: response.data.total,
                            //mainTags: response.data.mainTags,
                            //subTags: response.data.subTags
                            filterTags: response.data.filterTags
                        });

                        if (page == 0) {
                            setFilterTags(response.data.filterTags);
                        }
                    }
                });
        }
    };

    const commaSeparetedString = (arr: string[]): string => {
        return arr.join(",");
    }

    const queryCardCoodrinates = () => {
        let serviceReponse = getService();

        if (serviceReponse.service != null) {
            let tagString = commaSeparetedString(searchState.tags);
            let transactionTagString = commaSeparetedString(searchState.transactionTags);

            serviceReponse.service.getCardCoordinates(
                searchState.query,
                tagString,
                transactionTagString,
                serviceReponse.region,
                serviceReponse.languageCode,
                searchState.digital,
                searchState.openNow)
                .then((response: ICardCoordinate[]) => {
                    if (response != null) {
                        fillMapMarkers(response);
                    }
                });
        }
    }

    const fillMapMarkers = (cards: ICardCoordinate[]) => {
        let markers: IMapMarker[] = [];
        cards.forEach(c => {
            if (c.addressAndCoordinates != null && c.addressAndCoordinates.length > 0) {
                c.addressAndCoordinates.forEach(a => {
                    markers.push({
                        latitude: a.latitude,
                        longitude: a.longitude,
                        popup: { address: a.address, description: c.description, pageLink: c.detailPageLink, title: c.header },
                    });
                });
            }
        });
        setMapMarkers(markers);
    };

    const [mapZoomLatLng, setMapZoomLatLng] = useState<[number, number]>([0, 0]);
    const zoomToMarker = (latlng: [number, number]) => {
        setMapZoomLatLng(latlng);
    };

    const [positionLatLng, setPositionLatLng] = useState<[number, number] | null>();
    const positionSet = (latlng: [number, number]) => {
        setPositionLatLng(latlng);
    };

    return (
        <div>
            <SearchBar
                searchCallback={searchCallback}
                filterTags={filterTags}
                transactionTags={transactionTags} />
            <Map
                myPositionFitBound={false}
                markers={mapMarkers}
                viewLatLng={mapZoomLatLng}
                positionSet={positionSet} />
            <Cards
                cardResponse={cardResponse}
                paginationCallback={paginationCallback}
                currentPage={currentPage}
                zoomToMarker={zoomToMarker}
                positionSetLatLng={positionLatLng}
                sortingCallback={sortingChange} />
        </div>
    );
}

export default StartPage;