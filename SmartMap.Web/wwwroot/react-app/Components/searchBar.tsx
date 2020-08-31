import React, { FunctionComponent, useState, useEffect } from 'react';
import { ITag } from '../service-model';
import { getText } from '../Helpers/getText';
import { getService } from '../bussinessService';

enum TagType {
    Tag,
    TransactionForm,
}

export interface ISearchState {
    query: string;
    tags: string[];
    transactionTags: string[];
    digital: boolean;
    openNow: boolean;
}

export const ISearchStateDefaultValue: ISearchState = { query: '', tags: [], transactionTags: [], digital: false, openNow: false };

export interface ITextTranslation {
    searchButtonText: string;
    filterButtonText: string;
    srBusinesses: string;
    moreTagsButton: string;
    searchInputPlaceholderText: string;
    transactionHeaderText: string;
    miscHeaderText: string;
    chkboxDigitalText: string;
    chkboxOpenNowText: string;
    searchInputAriaLabel: string;
}

interface ISearchBarInput {
    searchCallback: (state: ISearchState) => void;
    filterTags: ITag[];
    transactionTags: ITag[];
}

export const SearchBar: FunctionComponent<ISearchBarInput> = (props: ISearchBarInput) => {
    const [queryState, setQueryState] = useState<string>('');
    const [digitalState, setDigitalState] = useState<boolean>(false);
    const [openNowState, setOpenNowState] = useState<boolean>(false);
    const [textTranslations, setTextTranslations] = useState<ITextTranslation>();
    
    const [transactionTagsState, setTransactionTagsState] = useState<string[]>([]);
    const [currentTagsPage, setCurrentTagsPage] = useState<number>(1);
    const [tagsState, setTagsState] = useState<string[]>([]);
    const [visibleTags, setVisibleTags] = useState<ITag[]>([]);

    const pageSize: number = 6; // tags

    useEffect(() => {
        let _service = getService();
        if (_service != null) {
            _service.getTranslations().then(translations => {
                setTextTranslations({
                    searchButtonText: getText('searchbar.search-button', translations, 'Sök'),
                    filterButtonText: getText('searchbar.filter-button', translations, 'Filter'),
                    srBusinesses: getText('searchbar.sr-only-businesses', translations, 'antal verksamheter'),
                    moreTagsButton: getText('searchbar.more-tags-button', translations, 'Fler taggar'),
                    searchInputPlaceholderText: getText('searchbar.search-input-placeholder', translations, 'Sök på något du vill hyra, byta, låna, dela, ge eller få...'),
                    transactionHeaderText: getText('searchbar.filter-transaction-header', translations, 'Transaktionsform'),
                    miscHeaderText: getText('searchbar.filter-misc-header', translations, 'Övrigt'),
                    chkboxDigitalText: getText('searchbar.filter-checkbox-digital', translations, 'Endast digitalt'),
                    chkboxOpenNowText: getText('searchbar.filter-checkbox-opennow', translations, 'Öppet nu'),
                    searchInputAriaLabel: getText('searchbar.search-input-aria-label', translations, 'sök verksamhet')
                });
                
            });
        }
        
        let fdd = document.getElementById("filter-menu-btn");
        if (fdd != null) fdd.addEventListener("click", openFilterMenu);
        
        let b = document.getElementsByTagName("body")[0];
        b.addEventListener("click", hideFilterMenu);

        return () => {
            let fdd = document.getElementById("filter-menu-btn");
            if (fdd != null) fdd.removeEventListener("click", openFilterMenu);

            let b = document.getElementsByTagName("body")[0];
            b.removeEventListener("click", hideFilterMenu);
        };
    }, []);

    useEffect(() => {
        setCurrentTagsPage(1);
        //let tags = paginate(props.filterTags, pageSize, 1);
        let tags = props.filterTags;
        setVisibleTags(tags);
    }, [props.filterTags]);

    useEffect(() => {
        if (currentTagsPage > 1) {
            let tags = paginate(props.filterTags, pageSize, currentTagsPage);
            setVisibleTags(visibleTags.concat(tags));
        }
    }, [currentTagsPage]);

    const openFilterMenu = (event: MouseEvent) : void => {
        let menu = document.getElementById('filter-menu');
        if (menu != null) menu.classList.toggle('show');
    };

    const hideFilterMenu = (e: Event) : void => {
        let menu = document.getElementById('filter-menu');
        if (menu != null && e.target != null) {
            let target: any = e.target;
            if (menu.classList.contains("show") && !target.classList.contains("dont-hide")) {
                menu.classList.remove('show');
            }
        }
    };

    const search = (query: string, tags: string[], transactionTags: string[], digital: boolean, openNow: boolean) => {
        props.searchCallback({
            query: query,
            tags: tags,
            transactionTags: transactionTags,
            digital: digital,
            openNow: openNow,
        });
    };

    const callSearch = () => {
        search(queryState, tagsState, transactionTagsState, digitalState, openNowState);
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => {
        if (e.key === 'Enter') {
            search(queryState, tagsState, transactionTagsState, digitalState, openNowState);
        }
    };

    const handleSearchChange = (e: React.FormEvent<HTMLInputElement>): void => {
        setQueryState(e.currentTarget.value);
    };

    const removeFromTagState = (tag: string, type: TagType) => {
        if (type == TagType.Tag) {
            if (tagsState.indexOf(tag) !== -1) {
                let tags = tagsState.filter((e: string) => e !== tag);
                setTagsState(tags);
                search(queryState, tags, transactionTagsState, digitalState, openNowState);
            }
        }
        else if (type == TagType.TransactionForm) {
            if (transactionTagsState.indexOf(tag) !== -1) {
                let tags = transactionTagsState.filter((e: string) => e !== tag);
                setTransactionTagsState(tags);
                search(queryState, tagsState, tags, digitalState, openNowState);
            }
        }

    };
    const addToTagState = (tag: string, type: TagType) => {
        if (type == TagType.Tag) {
            if (tagsState.indexOf(tag) === -1) {
                let tags = tagsState.concat([tag]);
                setTagsState(tags);
                search(queryState, tags, transactionTagsState, digitalState, openNowState);
            }
        }
        else if (type == TagType.TransactionForm) {
            if (transactionTagsState.indexOf(tag) === -1) {
                let tags = transactionTagsState.concat([tag]);
                setTransactionTagsState(tags);
                search(queryState, tagsState, tags, digitalState, openNowState);
            }
        }
    };

    const addRemoveFromTags = (tag: string): void => {
        if (tagsState.indexOf(tag) === -1) {
            addToTagState(tag, TagType.Tag);
        }
        else {
            removeFromTagState(tag, TagType.Tag);
        }
    }

    const tagClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void => {
        addRemoveFromTags(e.currentTarget.value);
    }

    const changeEvent = (e: React.ChangeEvent<HTMLInputElement>): void => {
        let isChecked : boolean = e.currentTarget.checked;
        let value : string = e.currentTarget.value;
        let id : string = e.currentTarget.id;

        if (id.indexOf('cct-') !== -1) {
            if (isChecked)
                addToTagState(value, TagType.TransactionForm);
            else
                removeFromTagState(value, TagType.TransactionForm);
        }

        if (id.indexOf('cc-') !== -1) {
            if (id === 'cc-digital') {
                console.log('digital', isChecked);
                setDigitalState(isChecked);
                search(queryState, tagsState, transactionTagsState, isChecked, openNowState);
            }
            if (id === 'cc-opennow') {
                console.log('opennow', isChecked);
                setOpenNowState(isChecked);
                search(queryState, tagsState, transactionTagsState, digitalState, isChecked);
            }
        }
    };

    // url: https://stackoverflow.com/a/42761393
    const paginate = (array: ITag[], page_size: number, page_number: number) : ITag[] => {
        // human-readable page numbers usually start with 1, so we reduce 1 in the first argument
        return array.slice((page_number - 1) * page_size, page_number * page_size);
    };

    const moreTags = (page: number) => {
        setCurrentTagsPage(page);
    };
    // const result = words.filter(word => word.length > 6);
    const renderTags = <div>
        {visibleTags.filter((t: ITag) => t.type == "main").map((t: ITag, i: number) =>
            <span key={t.id}>
                <button
                    type="button"
                    value={t.name}
                    onClick={tagClick}
                    className={`btn ${tagsState.indexOf(t.name) === -1 ? 'btn-outline-primary' : 'btn-primary'} btn-sm text-capitalize mb-2`}>
                    {t.name} 
                    {/* <span className="badge badge-primary">{t.count}</span> */}
                    <span className="sr-only">{textTranslations?.srBusinesses}</span>
                </button>&nbsp;
            </span>
        )}
        {/* {(pageSize * currentTagsPage) < props.filterTags.length &&
            <button type="button" value="more" onClick={() => moreTags(currentTagsPage + 1)} className={`btn btn-primary btn-sm text-capitalize mb-2`}>
                {textTranslations?.moreTagsButton}
            </button>
        } */}
    </div>

    const renderSubTags = <div>
        {visibleTags.filter((t: ITag) => t.type == "sub").map((t: ITag, i: number) =>
            <span key={t.id}>
                <button
                    type="button"
                    value={t.name}
                    onClick={tagClick}
                    className={`btn ${tagsState.indexOf(t.name) === -1 ? 'btn-outline-info' : 'btn-info'} btn-sm text-capitalize mb-2`}>
                    {t.name} 
                    {/* <span className="badge badge-secondary">{t.count}</span> */}
                    <span className="sr-only">{textTranslations?.srBusinesses}</span>
                </button>&nbsp;
            </span>
        )}
        {/* {(pageSize * currentTagsPage) < props.filterTags.length &&
            <button type="button" value="more" onClick={() => moreTags(currentTagsPage + 1)} className={`btn btn-primary btn-sm text-capitalize mb-2`}>
                {textTranslations?.moreTagsButton}
            </button>
        } */}
    </div>
    
    const renderTransactionTags = props.transactionTags.map((t, i) =>
        <div className="custom-control custom-checkbox mb-1 dont-hide" key={t.id}>
            <input type="checkbox" onChange={changeEvent} className="custom-control-input dont-hide" 
                id={`cct-${t.id}`} name={`cct-${t.id}`} value={t.name} />
            <label className="custom-control-label text-capitalize dont-hide" htmlFor={`cct-${t.id}`}>{t.name}</label>
        </div>
    );

    return <div className="searchbar-component">
        <div className="input-group mb-2">
            <input type="text"
                className="form-control"
                placeholder={textTranslations?.searchInputPlaceholderText}
                aria-label={textTranslations?.searchInputAriaLabel}
                aria-describedby="button-search"
                onChange={handleSearchChange}
                onKeyDown={handleKeyDown}
                value={queryState}
            />
            <div className="input-group-append">
                <button className="btn btn-primary" type="button" id="button-search" onClick={callSearch}>
                    <i className="fas fa-search"></i> {textTranslations?.searchButtonText}
                </button>

                {/* data-toggle="dropdown" */}
                <button type="button"
                    id="filter-menu-btn"
                    className="btn btn-primary dropdown-toggle btn-sm dont-hide"
                    
                    data-display="static"
                    aria-haspopup="true"
                    aria-expanded="false">
                    <i className="fas fa-filter dont-hide"></i> {textTranslations?.filterButtonText}
                </button>

                <div className="dropdown-menu dropdown-menu-right dont-hide" id="filter-menu">
                    <div className="px-4 py-3 dont-hide">
                        <div className="row dont-hide">
                            <div className="col-6 dont-hide">
                                <h6 className="dont-hide">{textTranslations?.transactionHeaderText}</h6>
                                <div className="form-group pt-1 dont-hide">
                                    {renderTransactionTags}
                                </div>
                            </div>
                            <div className="col-6 dont-hide">
                                <h6 className="dont-hide">{textTranslations?.miscHeaderText}</h6>
                                <div className="form-group pt-1 dont-hide">
                                    <div className="custom-control custom-checkbox mb-1 dont-hide">
                                        <input type="checkbox" onChange={changeEvent} className="custom-control-input dont-hide" id="cc-digital" name="cc-digital" value="digital" />
                                        <label className="custom-control-label text-capitalize dont-hide" htmlFor="cc-digital">{textTranslations?.chkboxDigitalText}</label>
                                    </div>
                                    <div className="custom-control custom-checkbox dont-hide">
                                        <input type="checkbox" onChange={changeEvent} className="custom-control-input dont-hide" id="cc-opennow" name="cc-opennow" value="opennow" />
                                        <label className="custom-control-label text-capitalize dont-hide" htmlFor="cc-opennow">{textTranslations?.chkboxOpenNowText}</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div>{renderTags}</div>
        <div>{renderSubTags}</div>
    </div>
}